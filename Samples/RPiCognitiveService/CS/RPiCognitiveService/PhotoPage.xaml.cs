using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Storage.Search;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RPiCognitiveService
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PhotoPage : Page
    {
        //Vision API key
        string key = "Your Vision API Key";  
        Size size_image;  //The size of the current image
        AnalysisResult thisresult;  //The result of analysis

        StorageFolder currentFolder;
        StorageFile Picker_SelectedFile;
        private QueryOptions queryOptions;

        private MediaCapture mediaCapture;
        private StorageFile photoFile;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";
        private bool isPreviewing;

        public PhotoPage()
        {
            this.InitializeComponent();
            queryOptions = new QueryOptions(CommonFileQuery.OrderByName, mediaFileExtensions);
            queryOptions.FolderDepth = FolderDepth.Shallow;
            isPreviewing = false;
            initCamera();
        }

        private string[] mediaFileExtensions = {
            // picture
            ".jpg",
            ".png",
            ".bmp",
        };

        private async void initCamera()
        {
            try
            {
                if (mediaCapture != null)
                {
                    // Cleanup MediaCapture object
                    if (isPreviewing)
                    {
                        await mediaCapture.StopPreviewAsync();
                        captureImage.Source = null;
                        isPreviewing = false;
                    }
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }

                txtLocation.Text = "Initializing camera to capture audio and video...";
                // Use default initialization
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                // Set callbacks for failure and recording limit exceeded
                txtLocation.Text = "Device successfully initialized for video recording!";
                mediaCapture.Failed += new MediaCaptureFailedEventHandler(mediaCapture_Failed);
                // Start Preview                
                previewElement.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
                txtLocation.Text = "Camera preview succeeded";

                // Enable buttons for video and photo capture
                btnTakePhoto.IsEnabled = true;

            }
            catch (Exception ex)
            {
                txtLocation.Text = "Unable to initialize camera for audio/video mode: " + ex.Message;
            }
        }

        private async void mediaCapture_Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    txtLocation.Text = "MediaCaptureFailed: " + currentFailure.Message;
                }
                catch (Exception)
                {
                }
                finally
                {
                    btnTakePhoto.IsEnabled = false;
                    txtLocation.Text += "\nCheck if camera is diconnected. Try re-launching the app";
                }
            });
        }

        private async void takePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnTakePhoto.IsEnabled = false;
                captureImage.Source = null;
                //Store image
                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                    PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);
                btnTakePhoto.IsEnabled = true;
                txtLocation.Text = "Take Photo succeeded: " + photoFile.Path;
                //Display image
                IRandomAccessStream photoStream = await photoFile.OpenReadAsync();
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(photoStream);
                captureImage.Source = bitmap;
                Picker_SelectedFile = photoFile;
                SelectFile();
            }
            catch (Exception ex)
            {
                txtLocation.Text = ex.Message;
                Cleanup();
            }
        }

        private async void Cleanup()
        {
            if (mediaCapture != null)
            {
                // Cleanup MediaCapture object
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                    captureImage.Source = null;
                    isPreviewing = false;
                }
                mediaCapture.Dispose();
                mediaCapture = null;
            }
            btnTakePhoto.IsEnabled = false;
        }

        /// <summary>
        /// Dispaly data
        /// </summary>
        /// <param name="result"></param>
        private void DisplayData(AnalysisResult result, bool init = true)
        {
            if (result == null)
                return;

            cvasMain.Children.Clear();
            var offset_h = 0.0; var offset_w = 0.0;
            var p = 0.0;
            var d = cvasMain.ActualHeight / cvasMain.ActualWidth;
            var d2 = size_image.Height / size_image.Width;
            if (d < d2)
            {
                offset_h = 0;
                offset_w = (cvasMain.ActualWidth - cvasMain.ActualHeight / d2) / 2;
                p = cvasMain.ActualHeight / size_image.Height;
            }
            else
            {
                offset_w = 0;
                offset_h = (cvasMain.ActualHeight - cvasMain.ActualWidth / d2) / 2;
                p = cvasMain.ActualWidth / size_image.Width;
            }
            if (result.Faces != null)
            {
                int count = 1;
                //Dispaly face
                foreach (var face in result.Faces)
                {
                    Windows.UI.Xaml.Shapes.Rectangle rect = new Windows.UI.Xaml.Shapes.Rectangle();
                    rect.Width = face.FaceRectangle.Width * p;
                    rect.Height = face.FaceRectangle.Height * p;
                    Canvas.SetLeft(rect, face.FaceRectangle.Left * p + offset_w);
                    Canvas.SetTop(rect, face.FaceRectangle.Top * p + offset_h);
                    rect.Stroke = new SolidColorBrush(Colors.Orange);
                    rect.StrokeThickness = 3;

                    cvasMain.Children.Add(rect);

                    TextBlock txt = new TextBlock();
                    txt.Foreground = new SolidColorBrush(Colors.Orange);
                    txt.Text = "#" + count;
                    Canvas.SetLeft(txt, face.FaceRectangle.Left * p + offset_w);
                    Canvas.SetTop(txt, face.FaceRectangle.Top * p + offset_h - 20);
                    cvasMain.Children.Add(txt);
                    count++;
                }
            }
            if (!init)
                return;

            //Display result to table
            if (result.Description != null && result.Description.Captions != null) //description
            {
                txtDesc.Text = result.Description.Captions[0].Text;
                txtDesc_Score.Text = Math.Round(result.Description.Captions[0].Confidence, 3).ToString();
            }
            if (result.Adult != null)  //adult content
            {
                txtAdult.Text = result.Adult.IsAdultContent.ToString();
                txtAdult_Score.Text = Math.Round(result.Adult.AdultScore, 3).ToString();

                txtRacy.Text = result.Adult.IsRacyContent.ToString();
                txtRacy_Score.Text = Math.Round(result.Adult.RacyScore, 3).ToString();
            }

            var list_child = gridTags.Children.ToList();  //Remove previous Tag data
            list_child.ForEach((e) =>
            {
                if (e as TextBlock != null && (e as TextBlock).Tag != null)
                {
                    gridTags.Children.Remove(e);
                }
            });

            list_child = gridFaces.Children.ToList();  //Remove previous face data
            list_child.ForEach((e) =>
            {
                if (e as TextBlock != null && (e as TextBlock).Tag != null)
                {
                    gridFaces.Children.Remove(e);
                }
            });

            if (result.Tags != null)  //Tag
            {
                int index = 1;
                foreach (var tag in result.Tags)
                {

                    TextBlock txt0 = new TextBlock();  //#
                    txt0.Text = "0" + index;
                    txt0.Padding = new Thickness(0);
                    Grid.SetRow(txt0, index + 1);
                    Grid.SetColumn(txt0, 0);
                    txt0.Tag = true;

                    TextBlock txt1 = new TextBlock();  //Tag Name
                    txt1.Text = tag.Name;
                    txt1.Padding = new Thickness(1);
                    Grid.SetRow(txt1, index + 1);
                    Grid.SetColumn(txt1, 1);
                    txt1.Tag = true;

                    TextBlock txt2 = new TextBlock();  //Tag Confidence
                    txt2.Text = Math.Round(tag.Confidence, 3).ToString();
                    txt2.Padding = new Thickness(1);
                    Grid.SetRow(txt2, index + 1);
                    Grid.SetColumn(txt2, 2);
                    txt2.Tag = true;

                    index++;

                    gridTags.Children.Add(txt0);
                    gridTags.Children.Add(txt1);
                    gridTags.Children.Add(txt2);
                }
            }

            if (result.Faces != null)  //faces
            {
                int index = 1;
                foreach (var face in result.Faces)
                {
                    TextBlock txt0 = new TextBlock();  //#
                    txt0.Text = "0" + index;
                    txt0.Padding = new Thickness(0);
                    Grid.SetRow(txt0, index + 1);
                    Grid.SetColumn(txt0, 0);
                    txt0.Tag = true;

                    TextBlock txt1 = new TextBlock();  //Age
                    txt1.Text = face.Age.ToString();
                    txt1.Padding = new Thickness(1);
                    Grid.SetRow(txt1, index + 1);
                    Grid.SetColumn(txt1, 1);
                    txt1.Tag = true;

                    TextBlock txt2 = new TextBlock();  //Sex
                    txt2.Text = face.Gender;
                    txt2.Padding = new Thickness(1);
                    Grid.SetRow(txt2, index + 1);
                    Grid.SetColumn(txt2, 2);
                    txt2.Tag = true;

                    index++;

                    gridFaces.Children.Add(txt0);
                    gridFaces.Children.Add(txt1);
                    gridFaces.Children.Add(txt2);
                }
            }

        }
        /// <summary>
        /// Re-rendering Data in main canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cvasMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DisplayData(thisresult, false);
        }

        private async void imgPhoto_ImageOpened(object sender, RoutedEventArgs e)
        {
            size_image = new Size((imgPhoto.Source as BitmapImage).PixelWidth, (imgPhoto.Source as BitmapImage).PixelHeight);

            VisionServiceClient client = new VisionServiceClient(key);
            var feature = new VisualFeature[] { VisualFeature.Tags, VisualFeature.Faces, VisualFeature.Description, VisualFeature.Adult, VisualFeature.Categories };

            var result = await client.AnalyzeImageAsync(txtLocation.Text, feature);
            thisresult = result;
            if (result != null)
            {
                DisplayData(result);
            }
            ringLoading.IsActive = false;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            Picker_Show();
        }

        private async void Picker_Show()
        {
            //SetMainPageControlEnableState(false);
            await Picker_Populate();
            grdPicker.Visibility = Visibility.Visible;
        }

        private async Task Picker_Populate()
        {
            Picker_SelectedFile = null;
            if (currentFolder == null)
            {
                lstFiles.Items.Clear();
                lstFiles.Items.Add(">Documents");
                lstFiles.Items.Add(">Pictures");
                lstFiles.Items.Add(">Music");
                lstFiles.Items.Add(">Videos");
                lstFiles.Items.Add(">RemovableStorage");
            }
            else
            {
                lstFiles.Items.Clear();
                lstFiles.Items.Add(">..");
                var folders = await currentFolder.GetFoldersAsync();
                foreach (var f in folders)
                {
                    lstFiles.Items.Add(">" + f.Name);
                }
                var query = currentFolder.CreateFileQueryWithOptions(queryOptions);
                var files = await query.GetFilesAsync();
                foreach (var f in files)
                {
                    lstFiles.Items.Add(f.Name);
                }
            }
        }
        //Clear Button Click Event
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            //lblError.Visibility = Visibility.Collapsed;
            txtFileName.Text = "";
        }

        ////Select Button Click Event
        private async void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (lstFiles.SelectedItem != null)
            {
                if (await Picker_BrowseTo(lstFiles.SelectedItem.ToString()))
                {
                    SelectFile();
                }
                else
                {
                    lstFiles.Focus(FocusState.Keyboard);
                }
            }
        }

        async void SelectFile()
        {
            Picker_Hide();
            try
            {
                if (Picker_SelectedFile != null)
                {
                    txtFileName.Text = Picker_SelectedFile.Path;
                    var stream = await Picker_SelectedFile.OpenAsync(Windows.Storage.FileAccessMode.Read);

                    var stream_send = stream.CloneStream();
                    var image = new BitmapImage();
                    image.SetSource(stream);
                    imgPhoto.Source = image;
                    size_image = new Size(image.PixelWidth, image.PixelHeight);

                    ringLoading.IsActive = true;
                    //Vision Service
                    VisionServiceClient client = new VisionServiceClient(key);
                    var feature = new VisualFeature[] { VisualFeature.Tags, VisualFeature.Faces, VisualFeature.Description, VisualFeature.Adult, VisualFeature.Categories };

                    var result = await client.AnalyzeImageAsync(stream_send.AsStream(), feature);
                    thisresult = result;
                    if (result != null)
                    {
                        DisplayData(result);
                    }

                    //hide preview
                    if (stpPreview.Visibility == Visibility.Collapsed)
                    {
                        stpPreview.Visibility = Visibility.Visible;
                        btnShow.Content = "Hide Preview";
                    }
                    else
                    {
                        stpPreview.Visibility = Visibility.Collapsed;
                        btnShow.Content = "Show Preview";
                    }

                    ringLoading.IsActive = false;
                }
            }
            catch (Exception ex)
            {
                //lblError.Text = ex.Message;
                //lblError.Visibility = Visibility.Visible;
            }
        }

        //Show Button Click Event
        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (stpPreview.Visibility == Visibility.Collapsed)
            {
                stpPreview.Visibility = Visibility.Visible;
                btnShow.Content = "Hide Preview";
            }
            else
            {
                stpPreview.Visibility = Visibility.Collapsed;
                btnShow.Content = "Show Preview";
            }
        }

        //Open Button Click Event
        private async void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //lblError.Visibility = Visibility.Collapsed;
                var file = await StorageFile.GetFileFromPathAsync(txtFileName.Text);
                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            }
            catch (Exception ex)
            {
                //lblError.Text = ex.Message;
                //lblError.Visibility = Visibility.Visible;
            }
        }

        private void txtFileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //lblError.Visibility = Visibility.Collapsed;
        }

        private void Picker_Hide()
        {
            SetMainPageControlEnableState(true);
            grdPicker.Visibility = Visibility.Collapsed;
        }

        private void SetMainPageControlEnableState(bool isEnabled)
        {
            btnBrowse.IsEnabled = isEnabled;
            btnClear.IsEnabled = isEnabled;
            btnOpen.IsEnabled = isEnabled;
            txtFileName.IsEnabled = isEnabled;
        }

        private async Task<bool> Picker_BrowseTo(string filename)
        {
            Picker_SelectedFile = null;
            if (currentFolder == null)
            {
                switch (filename)
                {
                    case ">Documents":
                        currentFolder = KnownFolders.DocumentsLibrary;
                        break;
                    case ">Pictures":
                        currentFolder = KnownFolders.PicturesLibrary;
                        break;
                    case ">Music":
                        currentFolder = KnownFolders.MusicLibrary;
                        break;
                    case ">Videos":
                        currentFolder = KnownFolders.VideosLibrary;
                        break;
                    case ">RemovableStorage":
                        currentFolder = KnownFolders.RemovableDevices;
                        break;
                    default:
                        throw new Exception("unexpected");
                }
                lblBreadcrumb.Text = "> " + filename.Substring(1);
            }
            else
            {
                if (filename == ">..")
                {
                    await Picker_FolderUp();
                }
                else if (filename[0] == '>')
                {
                    var foldername = filename.Substring(1);
                    var folder = await currentFolder.GetFolderAsync(foldername);
                    currentFolder = folder;
                    lblBreadcrumb.Text += " > " + foldername;
                }
                else
                {
                    Picker_SelectedFile = await currentFolder.GetFileAsync(filename);
                    return true;
                }
            }
            await Picker_Populate();
            return false;
        }

        async Task Picker_FolderUp()
        {
            if (currentFolder == null)
            {
                return;
            }
            try
            {
                var folder = await currentFolder.GetParentAsync();
                currentFolder = folder;
                if (currentFolder == null)
                {
                    lblBreadcrumb.Text = ">";
                }
                else
                {
                    var breadcrumb = lblBreadcrumb.Text;
                    breadcrumb = breadcrumb.Substring(0, breadcrumb.LastIndexOf('>') - 1);
                    lblBreadcrumb.Text = breadcrumb;
                }
            }
            catch (Exception)
            {
                currentFolder = null;
                lblBreadcrumb.Text = ">";
            }
        }

        //DoubleTapped event for listview
        private async void lstFiles_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (lstFiles.SelectedItem != null)
            {
                if (await Picker_BrowseTo(lstFiles.SelectedItem.ToString()))
                {
                    SelectFile();
                }
                else
                {
                    lstFiles.Focus(FocusState.Keyboard);
                }
            }
        }

        //KeyUp event for listview
        private async void lstFiles_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (lstFiles.SelectedItem != null && e.Key == Windows.System.VirtualKey.Enter)
            {
                if (await Picker_BrowseTo(lstFiles.SelectedItem.ToString()))
                {
                    SelectFile();
                }
                else
                {
                    lstFiles.Focus(FocusState.Keyboard);
                }
            }
        }

        //Cancel Button Click Event
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Picker_Hide();
        }
    }
}
