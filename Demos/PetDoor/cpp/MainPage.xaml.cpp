//
// MainPage.xaml.cpp
// Implementation of the MainPage class.
//

#include "pch.h"
#include "MainPage.xaml.h"
#include <ppltasks.h>
#include "MotionSensor.h"
#include "Servo.h"



using namespace PetDoor;

using namespace Concurrency;
using namespace cv;
using namespace Platform;

using namespace Microsoft::IoT::Lightning::Providers;
using namespace Microsoft::WRL;

using namespace std;

using namespace Windows::ApplicationModel::Core;

using namespace Windows::Devices;
using namespace Windows::Devices::Enumeration;
using namespace Windows::Devices::Sensors;

using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;

using namespace Windows::Graphics::Display;
using namespace Windows::Graphics::Imaging;

using namespace Windows::Media;

using namespace Windows::Storage;
using namespace Windows::Storage::Streams;

using namespace Windows::System::Threading;

using namespace Windows::UI::Core;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Controls::Primitives;
using namespace Windows::UI::Xaml::Data;
using namespace Windows::UI::Xaml::Input;
using namespace Windows::UI::Xaml::Interop;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Media::Imaging;
using namespace Windows::UI::Xaml::Navigation;



// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409


#define LEFT_SERVO 2 // 2nd channel on PC9685
#define RIGHT_SERVO 3 // 3rd channel on PCA9685
#define MOTION_SENSOR_PIN_OUTDOOR 26
#define MOTION_SENSOR_PIN_INDOOR 19
#define MOTION_SENSOR_TIMER_INTERVAL 1 // In seconds


MainPage::MainPage()
	: _mediaCapture(nullptr)
	, _isInitialized(false)
	, _isPreviewing(false)
	, _externalCamera(false)
	, _mirroringPreview(false)
	, _displayOrientation(DisplayOrientations::Portrait)
	, _displayRequest(ref new Windows::System::Display::DisplayRequest())
	, RotationKey({ 0xC380465D, 0x2271, 0x428C,{ 0x9B, 0x83, 0xEC, 0xEA, 0x3B, 0x4A, 0x85, 0xC1 } })
	, _captureFolder(nullptr)
{
	InitializeComponent();
	// load in the cat classifier
	const cv::String cat_cascade_name = "Assets/haarcascade_frontalcatface_extended.xml";
	_displayInformation = DisplayInformation::GetForCurrentView();
	_systemMediaControls = SystemMediaTransportControls::GetForCurrentView();

	if (!cat_cascade.load(cat_cascade_name)) {
		printf("Couldnt load cat detector '%s'\n", cat_cascade_name);
		exit(1);
	}

	// Cache the UI to have the checkboxes retain their state, as the enabled/disabled state of the
	// GetPreviewFrameButton is reset in code when suspending/navigating (see Start/StopPreviewAsync)
	Page::NavigationCacheMode = Navigation::NavigationCacheMode::Required;

	// Useful to know when to initialize/clean up the camera
	_applicationSuspendingEventToken =
		Application::Current->Suspending += ref new SuspendingEventHandler(this, &MainPage::Application_Suspending);
	_applicationResumingEventToken =
		Application::Current->Resuming += ref new EventHandler<Object^>(this, &MainPage::Application_Resuming);


	InitServos().then([this] {
		InitMotionSensors();
	});
	
}

MainPage::~MainPage() {
	Application::Current->Suspending -= _applicationSuspendingEventToken;
	Application::Current->Resuming -= _applicationResumingEventToken;
	_systemMediaControls->PropertyChanged -= _mediaControlPropChangedEventToken;
}

void MainPage::InitMotionSensors()
{
	motionSensorOutdoor = ref new MotionSensor(MOTION_SENSOR_PIN_OUTDOOR);
	motionSensorIndoor = ref new MotionSensor(MOTION_SENSOR_PIN_INDOOR);

	// Add event handlers
	motionSensorOutdoor->MotionDetected += ref new PetDoor::MotionDetectedEventHandler(this,
		&MainPage::OnOutdoorMotionDetected);
	motionSensorIndoor->MotionDetected += ref new PetDoor::MotionDetectedEventHandler(this,
		&MainPage::OnIndoorMotionDetected);
}

task<void> MainPage::InitServos()
{
	return create_task([this] {
		rightServo = ref new Servo(RIGHT_SERVO);
		leftServo = ref new Servo(LEFT_SERVO);
	});
}

// Called when motion is detected outdoors
void MainPage::OnOutdoorMotionDetected(Object^ sender, Platform::String^ s)
{
	OutputDebugString(L"Outdoor motion detected\n");
	// If preview is not running, no preview frames can be acquired
	if (!_isPreviewing) return;
	auto getFrameTask = GetPreviewFrameAsSoftwareBitmapAsync();
	// open the door if your cats are there (according to the model)
	getFrameTask.then([this](int cat_count) {
		if (cat_count > 0) {
			std::wstringstream catNo;
			catNo << "Cats found: " << cat_count << "\n";
			OutputDebugString(catNo.str().c_str());
			OpenDoor(3000);
		}
	});

}

// Turns the servo so the pet door can be opened
// by default, the door stays open for 5 seconds; pass in a different parameter if you'd like to change that
void MainPage::OpenDoor(int stayOpenMS = 5000)
{
	rightServo->Rotate(0.1300);
	leftServo->Rotate(0.0188);
	Sleep(700);
	Sleep(stayOpenMS);
	leftServo->Rotate(0.0765);
	rightServo->Rotate(0.0785);
	Sleep(700);
	leftServo->Stop();
	rightServo->Stop();
}

// Open the door when the cat wants to go out
void MainPage::OnIndoorMotionDetected(Object^ sender, Platform::String^ s)
{
	OpenDoor(3000);
	OutputDebugString(L"Indoor motion detected\n");
}

/// <summary>
/// Initializes the MediaCapture, registers events, gets camera device information for mirroring and rotating, and starts preview
/// </summary>
/// <returns></returns>
task<void> MainPage::InitializeCameraAsync()
{

	// Attempt to get the back camera if one is available, but use any camera device if not
	return FindCameraDeviceByPanelAsync(Windows::Devices::Enumeration::Panel::Back)
		.then([this](DeviceInformation^ camera)
	{
		if (camera == nullptr)
		{
			return;
		}
		// Figure out where the camera is located
		if (camera->EnclosureLocation == nullptr || camera->EnclosureLocation->Panel == Windows::Devices::Enumeration::Panel::Unknown)
		{
			// No information on the location of the camera, assume it's an external camera, not integrated on the device
			_externalCamera = true;
		}
		else
		{
			// Camera is fixed on the device
			_externalCamera = false;

			// Only mirror the preview if the camera is on the front panel
			_mirroringPreview = (camera->EnclosureLocation->Panel == Windows::Devices::Enumeration::Panel::Front);
		}

		_mediaCapture = ref new Capture::MediaCapture();

		// Register for a notification when something goes wrong
		_mediaCaptureFailedEventToken =
			_mediaCapture->Failed += ref new Capture::MediaCaptureFailedEventHandler(this, &MainPage::MediaCapture_Failed);

		auto settings = ref new Capture::MediaCaptureInitializationSettings();
		settings->VideoDeviceId = camera->Id;

		// Initialize media capture and start the preview	
		create_task(_mediaCapture->InitializeAsync(settings)).then([this]()
		{
			_isInitialized = true;

			return StartPreviewAsync();
			// Different return types, must do the error checking here since we cannot return and send
			// execeptions back up the chain.
		}).then([this](task<void> previousTask)
		{
			try
			{
				previousTask.get();
			}
			catch (AccessDeniedException^)
			{
				// Camera is denied access
			}
		});
	}).then([this]()
	{
		create_task(StorageLibrary::GetLibraryAsync(KnownLibraryId::Pictures))
			.then([this](StorageLibrary^ picturesLibrary)
		{
			_captureFolder = picturesLibrary->SaveFolder;
			if (_captureFolder == nullptr)
			{
				// In this case fall back to the local app storage since the Pictures Library is not available
				_captureFolder = ApplicationData::Current->LocalFolder;
			}
		});
	});
}

/// <summary>
/// Cleans up the camera resources (after stopping the preview if necessary) and unregisters from MediaCapture events
/// </summary>
/// <returns></returns>
task<void> MainPage::CleanupCameraAsync()
{

	std::vector<task<void>> taskList;

	if (_isInitialized)
	{
		if (_isPreviewing)
		{
			auto stopPreviewTask = create_task(StopPreviewAsync());
			taskList.push_back(stopPreviewTask);
		}

		_isInitialized = false;
	}

	// When all our tasks complete, clean up MediaCapture
	return when_all(taskList.begin(), taskList.end())
		.then([this]()
	{
		if (_mediaCapture.Get() != nullptr)
		{
			_mediaCapture->Failed -= _mediaCaptureFailedEventToken;
			_mediaCapture = nullptr;
			motionSensorOutdoor = nullptr;
			motionSensorIndoor = nullptr;
			leftServo = nullptr;
			rightServo = nullptr;
		}
	});
}

/// <summary>
/// Starts the preview and adjusts it for for rotation and mirroring after making a request to keep the screen on and unlocks the UI
/// </summary>
/// <returns></returns>
task<void> MainPage::StartPreviewAsync()
{

	// Prevent the device from sleeping while the preview is running
	_displayRequest->RequestActive();

	// Register to listen for media property changes
	_mediaControlPropChangedEventToken =
		_systemMediaControls->PropertyChanged += ref new TypedEventHandler<SystemMediaTransportControls^, SystemMediaTransportControlsPropertyChangedEventArgs^>(this, &MainPage::SystemMediaControls_PropertyChanged);

	// Set the preview source in the UI and mirror it if necessary
	PreviewControl->Source = _mediaCapture.Get();
	PreviewControl->FlowDirection = _mirroringPreview ? Windows::UI::Xaml::FlowDirection::RightToLeft : Windows::UI::Xaml::FlowDirection::LeftToRight;

	// Start the preview
	return create_task(_mediaCapture->StartPreviewAsync())
		.then([this](task<void> previousTask)
	{
		_isPreviewing = true;

		// Only need to update the orientation if the camera is mounted on the device
		if (!_externalCamera)
		{
			return SetPreviewRotationAsync();
		}

		// Not external, just return the previous task
		return previousTask;
	});
}

/// <summary>
/// Gets the current orientation of the UI in relation to the device and applies a corrective rotation to the preview
/// </summary>
task<void> MainPage::SetPreviewRotationAsync()
{
	// Calculate which way and how far to rotate the preview
	int rotationDegrees = ConvertDisplayOrientationToDegrees(_displayOrientation);

	// The rotation direction needs to be inverted if the preview is being mirrored
	if (_mirroringPreview)
	{
		rotationDegrees = (360 - rotationDegrees) % 360;
	}

	// Add rotation metadata to the preview stream to make sure the aspect ratio / dimensions match when rendering and getting preview frames
	auto props = _mediaCapture->VideoDeviceController->GetMediaStreamProperties(Capture::MediaStreamType::VideoPreview);
	props->Properties->Insert(RotationKey, rotationDegrees);
	return create_task(_mediaCapture->SetEncodingPropertiesAsync(Capture::MediaStreamType::VideoPreview, props, nullptr));
}

/// <summary>
/// Stops the preview and deactivates a display request, to allow the screen to go into power saving modes
/// </summary>
/// <returns></returns>
task<void> MainPage::StopPreviewAsync()
{
	_isPreviewing = false;

	return create_task(_mediaCapture->StopPreviewAsync())
		.then([this]()
	{
		// Use the dispatcher because this method is sometimes called from non-UI threads
		return Dispatcher->RunAsync(Windows::UI::Core::CoreDispatcherPriority::Normal, ref new Windows::UI::Core::DispatchedHandler([this]()
		{
			PreviewControl->Source = nullptr;
			// Allow the device screen to sleep now that the preview is stopped
			_displayRequest->RequestRelease();
		}));
	});
}

IBuffer^ IBufferFromArray(Array<unsigned char>^ data)
{
	DataWriter^ dataWriter = ref new DataWriter();
	dataWriter->WriteBytes(data);
	return dataWriter->DetachBuffer();
}

IBuffer^ IBufferFromPointer(LPBYTE pbData, DWORD cbData)
{
	auto byteArray = new ArrayReference<unsigned char>(pbData, cbData);
	return IBufferFromArray(reinterpret_cast<Array<unsigned char>^>(byteArray));
}

inline void ThrowIfFailed(HRESULT hr)
{
	if (FAILED(hr))
	{
		throw Platform::Exception::CreateException(hr);
	}
}

std::vector<unsigned char> getVectorFromBuffer(::Windows::Storage::Streams::IBuffer^ buf)
{
	auto reader = ::Windows::Storage::Streams::DataReader::FromBuffer(buf);

	std::vector<unsigned char> data(reader->UnconsumedBufferLength);

	if (!data.empty())
		reader->ReadBytes(
			::Platform::ArrayReference<unsigned char>(
				&data[0], data.size()));

	return data;
}

// Converts an image from a SoftwareBitmap to a Mat file. Mat files are used in OpenCV.
Mat* SoftwareBitmapToMat(SoftwareBitmap^ image)
{
	if (!image) return nullptr;
	int height = image->PixelHeight;
	int width = image->PixelWidth;
	int size = image->PixelHeight * image->PixelWidth * 4;


	byte* bytes = new byte[size];
	IBuffer^ buffer = IBufferFromPointer(bytes, size);
	image->CopyToBuffer(buffer);

	std::vector<unsigned char> data = getVectorFromBuffer(buffer);
	memcpy(bytes, data.data(), size);

	Mat* result = new Mat(height, width, CV_8UC4, bytes, cv::Mat::AUTO_STEP);

	return result;
}

unsigned char* GetPointerToPixelData(IBuffer^ buffer)
{
	ComPtr<IBufferByteAccess> bufferByteAccess;
	ComPtr<IInspectable> insp((IInspectable*)buffer);
	ThrowIfFailed(insp.As(&bufferByteAccess));

	unsigned char* pixels = nullptr;
	ThrowIfFailed(bufferByteAccess->Buffer(&pixels));

	return pixels;
}

/// <summary>
/// Converts an image from a Mat file to a Software Bitmap.
/// </summary>
SoftwareBitmap^ MatToSoftwareBitmap(cv::Mat image)
{
	if (!image.data) return nullptr;

	// Create the WriteableBitmap
	int height = image.rows;
	int width = image.cols;
	int size = height * width * image.step.buf[1];

	SoftwareBitmap^ result = ref new SoftwareBitmap(BitmapPixelFormat::Rgba8, width, height, BitmapAlphaMode::Ignore);

	byte* bytes = new byte[size];
	IBuffer^ buffer = IBufferFromPointer(bytes, size);

	unsigned char* dstPixels = GetPointerToPixelData(buffer);
	memcpy(dstPixels, image.data, image.step.buf[1] * image.cols*image.rows);

	result->CopyFromBuffer(buffer);

	return result;
}


/// <summary>
/// takes an image (inputImg), runs face and body classifiers on it, and stores the results in 
/// objectVector and objectVectorBodies, respectively
/// </summary>
void DetectObjects(cv::Mat& inputImg, std::vector<cv::Rect> & objectVector, cv::CascadeClassifier& cat_cascade)
{
	// create a matrix of unsigned 8-bit int of size rows x cols
	cv::Mat frame_gray = cv::Mat(inputImg.rows, inputImg.cols, CV_8UC4);

	cvtColor(inputImg, frame_gray, CV_RGBA2GRAY);
	cv::equalizeHist(frame_gray, frame_gray);

	// Detect cat faces
	cat_cascade.detectMultiScale(frame_gray, objectVector, 1.1, 5, 0 | CV_HAAR_SCALE_IMAGE, cv::Size(100, 100), cv::Size(300, 300));
}

void drawRectOverObjects(Mat& image, std::vector<cv::Rect>& objectVector)
{
	// Place a red rectangle around all detected objects in image
	for (unsigned int x = 0; x < objectVector.size(); x++)
	{
		cv::rectangle(image, objectVector[x], cv::Scalar(0, 0, 255, 255), 5);
		std::ostringstream catNo;
		catNo << "Cat #" << (x + 1);
		cv::putText(image, catNo.str(), cv::Point(objectVector[x].x, objectVector[x].y - 10), cv::FONT_HERSHEY_SIMPLEX, 0.55, (0, 0, 255), 2);
	}
}


/// <summary>
/// Gets the current preview frame as a SoftwareBitmap, displays its properties in a TextBlock, and can optionally display the image
/// in the UI and/or save it to disk as a jpg
/// </summary>
/// <returns></returns>
task<int> MainPage::GetPreviewFrameAsSoftwareBitmapAsync()
{
	// Get information about the preview
	auto previewProperties = static_cast<MediaProperties::VideoEncodingProperties^>(_mediaCapture->VideoDeviceController->GetMediaStreamProperties(Capture::MediaStreamType::VideoPreview));
	unsigned int videoFrameWidth = previewProperties->Width;
	unsigned int videoFrameHeight = previewProperties->Height;

	// Create the video frame to request a SoftwareBitmap preview frame
	auto videoFrame = ref new VideoFrame(BitmapPixelFormat::Rgba8, videoFrameWidth, videoFrameHeight);

	// Capture the preview frame
	return create_task(_mediaCapture->GetPreviewFrameAsync(videoFrame))
		.then([this](VideoFrame^ currentFrame)
	{
		// Collect the resulting frame
		auto previewFrame = currentFrame->SoftwareBitmap;
		BitmapPixelFormat framepPix = previewFrame->BitmapPixelFormat;
		Mat previewMat = *(SoftwareBitmapToMat(previewFrame));
		//SoftwareBitmap^ previewBitmap = previewFrame;
		std::vector<cv::Rect> objectVector;
		// Show the frame information
		std::wstringstream ss;
		ss << previewFrame->PixelWidth << "x" << previewFrame->PixelHeight << " " << previewFrame->BitmapPixelFormat.ToString()->Data();
		auto str = ss.str().c_str();
		// Update UI
		CoreApplication::MainView->CoreWindow->Dispatcher->RunAsync(
			CoreDispatcherPriority::High,
			ref new DispatchedHandler([this, str]()
			{
				FrameInfoTextBlock->Text = ref new Platform::String(str);
			}));

		// Use openCV to draw rectangles over detected objects

		DetectObjects(previewMat, objectVector, cat_cascade);
		drawRectOverObjects(previewMat, objectVector);
		previewFrame = SoftwareBitmap::Convert(MatToSoftwareBitmap(previewMat), BitmapPixelFormat::Bgra8);
		framepPix = previewFrame->BitmapPixelFormat;

		CoreApplication::MainView->CoreWindow->Dispatcher->RunAsync(
			CoreDispatcherPriority::High,
			ref new DispatchedHandler([this, previewFrame, currentFrame, objectVector]()
			{
				//taskList.push_back(UpdateAndSaveImage(previewFrame));
				UpdateAndSaveImage(previewFrame).then([currentFrame]() {
					// IClosable.Close projects into CX as operator delete.
					delete currentFrame;
				});
			}));

		// return the number of cats found
		return static_cast<int>(objectVector.size());



	});
}

task<void> MainPage::UpdateAndSaveImage(SoftwareBitmap ^previewFrame)
{
	auto sbSource = ref new Media::Imaging::SoftwareBitmapSource();
	return create_task(sbSource->SetBitmapAsync(previewFrame))
		.then([this, sbSource]()
	{
		// Display it in the Image control
		PreviewFrameImage->Source = sbSource;

	}).then([this, previewFrame]() 
	{
		SaveSoftwareBitmapAsync(previewFrame);
	});
}

/// <summary>
/// Saves a SoftwareBitmap to the _captureFolder
/// </summary>
/// <param name="bitmap"></param>
/// <returns></returns>
task<void> MainPage::SaveSoftwareBitmapAsync(SoftwareBitmap^ bitmap)
{
	return create_task(_captureFolder->CreateFileAsync("PreviewFrame.jpg", CreationCollisionOption::GenerateUniqueName))
		.then([bitmap](StorageFile^ file)
	{
		return create_task(file->OpenAsync(FileAccessMode::ReadWrite));
	}).then([this, bitmap](Streams::IRandomAccessStream^ outputStream)
	{
		return create_task(BitmapEncoder::CreateAsync(BitmapEncoder::JpegEncoderId, outputStream))
			.then([bitmap](BitmapEncoder^ encoder)
		{
			// Grab the data from the SoftwareBitmap
			encoder->SetSoftwareBitmap(bitmap);
			return create_task(encoder->FlushAsync());
		}).then([this, outputStream](task<void> previousTask)
		{
			// IClosable.Close projects into CX as operator delete.
			delete outputStream;
			try
			{
				previousTask.get();
			}
			catch (Platform::Exception^ ex)
			{
				// File I/O errors are reported as exceptions
				WriteException(ex);
			}
		});
	});
}


/// <summary>
/// Queries the available video capture devices to try and find one mounted on the desired panel
/// </summary>
/// <param name="desiredPanel">The panel on the device that the desired camera is mounted on</param>
/// <returns>A DeviceInformation instance with a reference to the camera mounted on the desired panel if available,
///          any other camera if not, or null if no camera is available.</returns>
task<DeviceInformation^> MainPage::FindCameraDeviceByPanelAsync(Windows::Devices::Enumeration::Panel panel)
{
	// Get available devices for capturing pictures
	auto allVideoDevices = DeviceInformation::FindAllAsync(DeviceClass::VideoCapture);

	auto deviceEnumTask = create_task(allVideoDevices);
	return deviceEnumTask.then([panel](DeviceInformationCollection^ devices)
	{
		for (auto cameraDeviceInfo : devices)
		{
			if (cameraDeviceInfo->EnclosureLocation != nullptr && cameraDeviceInfo->EnclosureLocation->Panel == panel)
			{
				return cameraDeviceInfo;
			}
		}

		// Nothing matched, just return the first
		if (devices->Size > 0)
		{
			return devices->GetAt(0);
		}

		// We didn't find any devices, so return a null instance
		DeviceInformation^ camera = nullptr;
		return camera;
	});
}

/// <summary>
/// Writes a given exception message and hresult to the output window
/// </summary>
/// <param name="ex">Exception to be written</param>
void MainPage::WriteException(Platform::Exception^ ex)
{
	std::wstringstream wStringstream;
	wStringstream << "0x" << ex->HResult << ": " << ex->Message->Data();
	OutputDebugString(wStringstream.str().c_str());
}

/// <summary>
/// Converts the given orientation of the app on the screen to the corresponding rotation in degrees
/// </summary>
/// <param name="orientation">The orientation of the app on the screen</param>
/// <returns>An orientation in degrees</returns>
int MainPage::ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
{
	switch (orientation)
	{
	case DisplayOrientations::Portrait:
		return 90;
	case DisplayOrientations::LandscapeFlipped:
		return 180;
	case DisplayOrientations::PortraitFlipped:
		return 270;
	case DisplayOrientations::Landscape:
	default:
		return 0;
	}
}

void MainPage::Application_Suspending(Object^, Windows::ApplicationModel::SuspendingEventArgs^ e)
{
	// Handle global application events only if this page is active
	if (Frame->CurrentSourcePageType.Name == Interop::TypeName(MainPage::typeid).Name)
	{
		_displayInformation->OrientationChanged -= _displayInformationEventToken;
		auto deferral = e->SuspendingOperation->GetDeferral();
		CleanupCameraAsync()
			.then([this, deferral]()
		{
			deferral->Complete();
		});
	}
}

void MainPage::Application_Resuming(Object^, Object^)
{
	// Handle global application events only if this page is active
	if (Frame->CurrentSourcePageType.Name == Interop::TypeName(MainPage::typeid).Name)
	{
		// Populate orientation variables with the current state and register for future changes
		_displayOrientation = _displayInformation->CurrentOrientation;
		_displayInformationEventToken =
			_displayInformation->OrientationChanged += ref new TypedEventHandler<DisplayInformation^, Object^>(this, &MainPage::DisplayInformation_OrientationChanged);

		InitializeCameraAsync();
	}
}

/// <summary>
/// In the event of the app being minimized this method handles media property change events. If the app receives a mute
/// notification, it is no longer in the foregroud.
/// </summary>
/// <param name="sender"></param>
/// <param name="args"></param>
void MainPage::SystemMediaControls_PropertyChanged(SystemMediaTransportControls^ sender, SystemMediaTransportControlsPropertyChangedEventArgs^ args)
{
	Dispatcher->RunAsync(Windows::UI::Core::CoreDispatcherPriority::Normal, ref new Windows::UI::Core::DispatchedHandler([this, sender, args]()
	{
		// Only handle this event if this page is currently being displayed
		if (args->Property == SystemMediaTransportControlsProperty::SoundLevel && Frame->CurrentSourcePageType.Name == Interop::TypeName(MainPage::typeid).Name)
		{
			// Check to see if the app is being muted. If so, it is being minimized.
			// Otherwise if it is not initialized, it is being brought into focus.
			if (sender->SoundLevel == SoundLevel::Muted)
			{
				CleanupCameraAsync();
			}
			else if (!_isInitialized)
			{
				InitializeCameraAsync();
			}
		}
	}));
}

/// <summary>
/// This event will fire when the page is rotated
/// </summary>
/// <param name="sender">The event source.</param>
/// <param name="args">The event data.</param>
void MainPage::DisplayInformation_OrientationChanged(DisplayInformation^ sender, Object^)
{
	_displayOrientation = sender->CurrentOrientation;

	if (_isPreviewing)
	{
		SetPreviewRotationAsync();
	}
}

void MainPage::MediaCapture_Failed(Capture::MediaCapture^, Capture::MediaCaptureFailedEventArgs^ errorEventArgs)
{
	// MediaCapture Failed!

	CleanupCameraAsync();
}

void MainPage::OnNavigatedTo(Windows::UI::Xaml::Navigation::NavigationEventArgs^)
{
	// Populate orientation variables with the current state and register for future changes
	_displayOrientation = _displayInformation->CurrentOrientation;
	_displayInformationEventToken =
		_displayInformation->OrientationChanged += ref new TypedEventHandler<DisplayInformation^, Object^>(this, &MainPage::DisplayInformation_OrientationChanged);
	InitializeCameraAsync();
}

void MainPage::OnNavigatingFrom(Windows::UI::Xaml::Navigation::NavigatingCancelEventArgs^)
{
	// Handling of this event is included for completeness, as it will only fire when navigating between pages and this sample only includes one page
	CleanupCameraAsync();

	_displayInformation->OrientationChanged -= _displayInformationEventToken;
}



