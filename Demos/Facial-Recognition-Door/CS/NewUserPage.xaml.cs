using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using FacialRecognitionDoor.Helpers;
using Windows.Storage;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace FacialRecognitionDoor
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewUserPage : Page
    {
        private WebcamHelper webcam;

        private StorageFile currentIdPhotoFile;

        public NewUserPage()
        {
            this.InitializeComponent();

            // If user has set the DisableLiveCameraFeed within Constants.cs to true, disable the feed:
            if (GeneralConstants.DisableLiveCameraFeed)
            {
                WebcamFeed.Visibility = Visibility.Collapsed;
                DisabledFeedGrid.Visibility = Visibility.Visible;
            } 
            else
            {
                WebcamFeed.Visibility = Visibility.Visible;
                DisabledFeedGrid.Visibility = Visibility.Collapsed;
            }         
        }

        /// <summary>
        /// Triggered every time the page is navigated to.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                //Sets passed through WecamHelper from MainPage as local webcam object
                webcam = e.Parameter as WebcamHelper;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Error when navigating to NewUserPage: " + exception.Message);
            }
        }

        /// <summary>
        /// Triggered when the webcam feed control is loaded. Sets up the live webcam feed.
        /// </summary>
        private async void WebcamFeed_Loaded(object sender, RoutedEventArgs e)
        {
            WebcamFeed.Source = webcam.mediaCapture;

            // Check to make sure MediaCapture isn't null before attempting to start preview. Will be null if no camera is attached.
            if (WebcamFeed.Source != null)
            {
                await webcam.StartCameraPreview();
            }
        }

        /// <summary>
        /// Triggered when the Capture Photo button is clicked by the user
        /// </summary>
        private async void Capture_Click(object sender, RoutedEventArgs e)
        {
            // Hide the capture photo button
            CaptureButton.Visibility = Visibility.Collapsed;

            // Capture current frame from webcam, store it in temporary storage and set the source of a BitmapImage to said photo
            currentIdPhotoFile = await webcam.CapturePhoto();
            var photoStream = await currentIdPhotoFile.OpenAsync(FileAccessMode.ReadWrite);
            BitmapImage idPhotoImage = new BitmapImage();
            await idPhotoImage.SetSourceAsync(photoStream);
            

            // Set the soruce of the photo control the new BitmapImage and make the photo control visible
            IdPhotoControl.Source = idPhotoImage;
            IdPhotoControl.Visibility = Visibility.Visible;

            // Collapse the webcam feed or disabled feed grid. Make the enter user name grid visible.
            WebcamFeed.Visibility = Visibility.Collapsed;
            DisabledFeedGrid.Visibility = Visibility.Collapsed;

            UserNameGrid.Visibility = Visibility.Visible;
            

            // Dispose photo stream
            photoStream.Dispose();
        }

        /// <summary>
        /// Triggered when the Confirm photo button is clicked by the user. Stores the captured photo to storage and navigates back to MainPage.
        /// </summary>
        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(UserNameBox.Text))
            {
                // Create or open the folder in which the Whitelist is stored
                StorageFolder whitelistFolder = await KnownFolders.PicturesLibrary.CreateFolderAsync(GeneralConstants.WhiteListFolderName, CreationCollisionOption.OpenIfExists);
                // Create a folder to store this specific user's photos
                StorageFolder currentFolder = await whitelistFolder.CreateFolderAsync(UserNameBox.Text, CreationCollisionOption.ReplaceExisting);
                // Move the already captured photo the user's folder
                await currentIdPhotoFile.MoveAsync(currentFolder);
                
                // Add user to Oxford database
                OxfordFaceAPIHelper.AddUserToWhitelist(UserNameBox.Text, currentFolder);

                // Stop live camera feed
                await webcam.StopCameraPreview();
                // Navigate back to MainPage
                Frame.Navigate(typeof(MainPage));
            }
        }

        /// <summary>
        /// Triggered when the Cancel Photo button is clicked by the user. Resets page.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Collapse the confirm photo buttons and open the capture photo button.
            CaptureButton.Visibility = Visibility.Visible;
            UserNameGrid.Visibility = Visibility.Collapsed;
            UserNameBox.Text = "";

            // Open the webcam feed or disabled camera feed
            if(GeneralConstants.DisableLiveCameraFeed)
            {
                DisabledFeedGrid.Visibility = Visibility.Visible;
            }
            else
            {
                WebcamFeed.Visibility = Visibility.Visible;
            }
            
            // Collapse the photo control:
            IdPhotoControl.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Triggered when the "Back" button is clicked by the user
        /// </summary>
        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop the camera preview
            await webcam.StopCameraPreview();
            // Navigate back to the MainPage
            Frame.Navigate(typeof(MainPage));
        }
    }
}
