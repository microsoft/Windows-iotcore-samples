using FacialRecognitionDoor.FacialRecognition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using FacialRecognitionDoor.Helpers;
using FacialRecognitionDoor.Objects;
using Microsoft.ProjectOxford.Face;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FacialRecognitionDoor
{
    public sealed partial class MainPage : Page
    {
        // Webcam Related Variables:
        private WebcamHelper webcam;

        // Oxford Related Variables:
        private bool initializedOxford = false;

        // Whitelist Related Variables:
        private List<Visitor> whitelistedVisitors = new List<Visitor>();
        private StorageFolder whitelistFolder;
        private bool currentlyUpdatingWhitelist;

        // Speech Related Variables:
        private SpeechHelper speech;

        // GPIO Related Variables:
        private GpioHelper gpioHelper;
        private bool gpioAvailable;
        private bool doorbellJustPressed = false;

        // GUI Related Variables:
        private double visitorIDPhotoGridMaxWidth = 0;

        /// <summary>
        /// Called when the page is first navigated to.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            // Causes this page to save its state when navigating to other pages
            NavigationCacheMode = NavigationCacheMode.Enabled;

            if (initializedOxford == false)
            {
                // If Oxford facial recognition has not been initialized, attempt to initialize it
                InitializeOxford();
            }

            if(gpioAvailable == false)
            {
                // If GPIO is not available, attempt to initialize it
                InitializeGpio();
            }

            // If user has set the DisableLiveCameraFeed within Constants.cs to true, disable the feed:
            if(GeneralConstants.DisableLiveCameraFeed)
            {
                LiveFeedPanel.Visibility = Visibility.Collapsed;
                DisabledFeedGrid.Visibility = Visibility.Visible;
            }
            else
            {
                LiveFeedPanel.Visibility = Visibility.Visible;
                DisabledFeedGrid.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Triggered every time the page is navigated to.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(initializedOxford)
            {
                UpdateWhitelistedVisitors();
            }
        }

        /// <summary>
        /// Called once, when the app is first opened. Initializes Oxford facial recognition.
        /// </summary>
        public async void InitializeOxford()
        {
            // initializedOxford bool will be set to true when Oxford has finished initialization successfully
            initializedOxford = await OxfordFaceAPIHelper.InitializeOxford();

            // Populates UI grid with whitelisted visitors
            UpdateWhitelistedVisitors();
        }

        /// <summary>
        /// Called once, when the app is first opened. Initializes device GPIO.
        /// </summary>
        public void InitializeGpio()
        {
            try
            {
                // Attempts to initialize application GPIO. 
                gpioHelper = new GpioHelper();
                gpioAvailable = gpioHelper.Initialize();
            }
            catch
            {
                // This can fail if application is run on a device, such as a laptop, that does not have a GPIO controller
                gpioAvailable = false;
                Debug.WriteLine("GPIO controller not available.");
            }

            // If initialization was successfull, attach doorbell pressed event handler
            if (gpioAvailable)
            {
                gpioHelper.GetDoorBellPin().ValueChanged += DoorBellPressed;
            }
        }

        /// <summary>
        /// Triggered when webcam feed loads both for the first time and every time page is navigated to.
        /// If no WebcamHelper has been created, it creates one. Otherwise, simply restarts webcam preview feed on page.
        /// </summary>
        private async void WebcamFeed_Loaded(object sender, RoutedEventArgs e)
        {
            if (webcam == null || !webcam.IsInitialized())
            {
                // Initialize Webcam Helper
                webcam = new WebcamHelper();
                await webcam.InitializeCameraAsync();

                // Set source of WebcamFeed on MainPage.xaml
                WebcamFeed.Source = webcam.mediaCapture;

                // Check to make sure MediaCapture isn't null before attempting to start preview. Will be null if no camera is attached.
                if (WebcamFeed.Source != null)
                {
                    // Start the live feed
                    await webcam.StartCameraPreview();
                }
            }
            else if(webcam.IsInitialized())
            {
                WebcamFeed.Source = webcam.mediaCapture;

                // Check to make sure MediaCapture isn't null before attempting to start preview. Will be null if no camera is attached.
                if (WebcamFeed.Source != null)
                {
                    await webcam.StartCameraPreview();
                }
            }
        }

        /// <summary>
        /// Triggered when media element used to play synthesized speech messages is loaded.
        /// Initializes SpeechHelper and greets user.
        /// </summary>
        private async void speechMediaElement_Loaded(object sender, RoutedEventArgs e)
        {
            if (speech == null)
            {
                speech = new SpeechHelper(speechMediaElement);
                await speech.Read(SpeechContants.InitialGreetingMessage);
            }
            else
            {
                // Prevents media element from re-greeting visitor
                speechMediaElement.AutoPlay = false;
            }
        }

        /// <summary>
        /// Triggered when the whitelisted users grid is loaded. Sets the size of each photo within the grid.
        /// </summary>
        private void WhitelistedUsersGrid_Loaded(object sender, RoutedEventArgs e)
        {
            visitorIDPhotoGridMaxWidth = (WhitelistedUsersGrid.ActualWidth / 3) - 10;
        }

        /// <summary>
        /// Triggered when user presses physical door bell button
        /// </summary>
        private async void DoorBellPressed(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (!doorbellJustPressed)
            {
                // Checks to see if even was triggered from a press or release of button
                if (args.Edge == GpioPinEdge.FallingEdge)
                {
                    //Doorbell was just pressed
                    doorbellJustPressed = true;

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await DoorbellPressed();
                    });

                }
            }
        }

        /// <summary>
        /// Triggered when user presses virtual doorbell app bar button
        /// </summary>
        private async void DoorbellButton_Click(object sender, RoutedEventArgs e)
        {
            if (!doorbellJustPressed)
            {
                doorbellJustPressed = true;
                await DoorbellPressed();
            }
        }

        /// <summary>
        /// Called when user hits physical or vitual doorbell buttons. Captures photo of current webcam view and sends it to Oxford for facial recognition processing.
        /// </summary>
        private async Task DoorbellPressed()
        {
            // Display analysing visitors grid to inform user that doorbell press was registered
            AnalysingVisitorGrid.Visibility = Visibility.Visible;

            // List to store visitors recognized by Oxford Face API
            // Count will be greater than 0 if there is an authorized visitor at the door
            List<string> recognizedVisitors = new List<string>();

            // Confirms that webcam has been properly initialized and oxford is ready to go
            if (webcam.IsInitialized() && initializedOxford)
            {
                // Stores current frame from webcam feed in a temporary folder
                StorageFile image = await webcam.CapturePhoto();

                try
                {
                    // Oxford determines whether or not the visitor is on the Whitelist and returns true if so
                    recognizedVisitors = await OxfordFaceAPIHelper.IsFaceInWhitelist(image);                    
                }
                catch (FaceRecognitionException fe)
                {
                    switch (fe.ExceptionType)
                    {
                        // Fails and catches as a FaceRecognitionException if no face is detected in the image
                        case FaceRecognitionExceptionType.NoFaceDetected:
                            Debug.WriteLine("WARNING: No face detected in this image.");
                            break;
                    }
                }
                catch (FaceAPIException faceAPIEx)
                {
                    Debug.WriteLine("FaceAPIException in IsFaceInWhitelist(): " + faceAPIEx.ErrorMessage);
                }
                catch
                {
                    // General error. This can happen if there are no visitors authorized in the whitelist
                    Debug.WriteLine("WARNING: Oxford just threw a general expception.");
                }
                
                if(recognizedVisitors.Count > 0)
                {
                    // If everything went well and a visitor was recognized, unlock the door:
                    UnlockDoor(recognizedVisitors[0]);
                }
                else
                {
                    // Otherwise, inform user that they were not recognized by the system
                    await speech.Read(SpeechContants.VisitorNotRecognizedMessage);
                }
            }
            else
            {
                if (!webcam.IsInitialized())
                {
                    // The webcam has not been fully initialized for whatever reason:
                    Debug.WriteLine("Unable to analyze visitor at door as the camera failed to initlialize properly.");
                    await speech.Read(SpeechContants.NoCameraMessage);
                }

                if(!initializedOxford)
                {
                    // Oxford is still initializing:
                    Debug.WriteLine("Unable to analyze visitor at door as Oxford Facial Recogntion is still initializing.");
                }
            }

            doorbellJustPressed = false;
            AnalysingVisitorGrid.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Unlocks door and greets visitor
        /// </summary>
        private async void UnlockDoor(string visitorName)
        {
            // Greet visitor
            await speech.Read(SpeechContants.GeneralGreetigMessage(visitorName));

            if(gpioAvailable)
            {
                // Unlock door for specified ammount of time
                gpioHelper.UnlockDoor();
            }
        }

        /// <summary>
        /// Called when user hits vitual add user button. Navigates to NewUserPage page.
        /// </summary>
        private async void NewUserButton_Click(object sender, RoutedEventArgs e)
        { 
            // Stops camera preview on this page, so that it can be started on NewUserPage
            await webcam.StopCameraPreview();

            //Navigates to NewUserPage, passing through initialized WebcamHelper object
            Frame.Navigate(typeof(NewUserPage), webcam);
        }

        /// <summary>
        /// Updates internal list of of whitelisted visitors (whitelistedVisitors) and the visible UI grid
        /// </summary>
        private async void UpdateWhitelistedVisitors()
        {
            // If the whitelist isn't already being updated, update the whitelist
            if (!currentlyUpdatingWhitelist)
            {
                currentlyUpdatingWhitelist = true;
                await UpdateWhitelistedVisitorsList();
                UpdateWhitelistedVisitorsGrid();
                currentlyUpdatingWhitelist = false;
            }
        }

        /// <summary>
        /// Updates the list of Visitor objects with all whitelisted visitors stored on disk
        /// </summary>
        private async Task UpdateWhitelistedVisitorsList()
        {
            // Clears whitelist
            whitelistedVisitors.Clear();

            // If the whitelistFolder has not been opened, open it
            if (whitelistFolder == null)
            {
                whitelistFolder = await KnownFolders.PicturesLibrary.CreateFolderAsync(GeneralConstants.WhiteListFolderName, CreationCollisionOption.OpenIfExists);
            }

            // Populates subFolders list with all sub folders within the whitelist folders.
            // Each of these sub folders represents the Id photos for a single visitor.
            var subFolders = await whitelistFolder.GetFoldersAsync();

            // Iterate all subfolders in whitelist
            foreach (StorageFolder folder in subFolders)
            {
                string visitorName = folder.Name;
                var filesInFolder = await folder.GetFilesAsync();

                var photoStream = await filesInFolder[0].OpenAsync(FileAccessMode.Read);
                BitmapImage visitorImage = new BitmapImage();
                await visitorImage.SetSourceAsync(photoStream);

                Visitor whitelistedVisitor = new Visitor(visitorName, folder, visitorImage, visitorIDPhotoGridMaxWidth);

                whitelistedVisitors.Add(whitelistedVisitor);
            }
        }

        /// <summary>
        /// Updates UserInterface list of whitelisted users from the list of Visitor objects (WhitelistedVisitors)
        /// </summary>
        private void UpdateWhitelistedVisitorsGrid()
        {
            // Reset source to empty list
            WhitelistedUsersGrid.ItemsSource = new List<Visitor>();
            // Set source of WhitelistedUsersGrid to the whitelistedVisitors list
            WhitelistedUsersGrid.ItemsSource = whitelistedVisitors;

            // Hide Oxford loading ring
            OxfordLoadingRing.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Triggered when the user selects a visitor in the WhitelistedUsersGrid 
        /// </summary>
        private void WhitelistedUsersGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to UserProfilePage, passing through the selected Visitor object and the initialized WebcamHelper as a parameter
            Frame.Navigate(typeof(UserProfilePage), new UserProfileObject(e.ClickedItem as Visitor, webcam));
        }

        /// <summary>
        /// Triggered when the user selects the Shutdown button in the app bar. Closes app.
        /// </summary>
        private void ShutdownButton_Click(object sender, RoutedEventArgs e)
        {
            // Exit app
            Application.Current.Exit();
        }
    }
}
