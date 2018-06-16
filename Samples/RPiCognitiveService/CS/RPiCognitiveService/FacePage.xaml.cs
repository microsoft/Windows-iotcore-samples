using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI;
using System.Threading.Tasks;
using Windows.Storage.Search;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RPiCognitiveService
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FacePage : Page
    {
        //Face API Key
        string key_face = "Your Face API Key";
        string face_apiroot = "Your API Endpoint" // For instance: https://westeurope.api.cognitive.microsoft.com/face/v1.0

        Size size_image;
        Face[] faces;

        StorageFolder currentFolder;
        StorageFile Picker_SelectedFile;
        private QueryOptions queryOptions;

        private MediaCapture mediaCapture;
        private StorageFile photoFile;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";
        private bool isPreviewing;

        public FacePage()
        {
            this.InitializeComponent();
            queryOptions = new QueryOptions(CommonFileQuery.OrderByName, mediaFileExtensions);
            queryOptions.FolderDepth = FolderDepth.Shallow;
            isPreviewing = false;
            initCamera();
        }

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
                //store the captured image
                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                    PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);
                btnTakePhoto.IsEnabled = true;
                txtLocation.Text = "Take Photo succeeded: " + photoFile.Path;
                //display the image
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


        private string[] mediaFileExtensions = {
            // picture
            ".jpg",
            ".png",
            ".bmp",
        };

        //Browse Button Click Event
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            Picker_Show();
        }

        private async void Picker_Show()
        {
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
            txtFileName.Text = "";
        }

        //Select Button Click Event
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
                    var stream_send2 = stream.CloneStream();
                    var image = new BitmapImage();
                    image.SetSource(stream);
                    imgPhoto.Source = image;
                    size_image = new Size(image.PixelWidth, image.PixelHeight);

                    ringLoading.IsActive = true;

                    //Face service
                    FaceServiceClient f_client = new FaceServiceClient(key_face, face_apiroot);

                    var requiedFaceAttributes = new FaceAttributeType[] {
                                FaceAttributeType.Age,
                                FaceAttributeType.Gender,
                                FaceAttributeType.Smile,
                                FaceAttributeType.FacialHair,
                                FaceAttributeType.HeadPose,
                                FaceAttributeType.Emotion,
                                FaceAttributeType.Glasses
                                };
                    var faces_task = f_client.DetectAsync(stream_send.AsStream(), true, true, requiedFaceAttributes);

                    faces = await faces_task;

                    if (faces != null)
                    {
                        DisplayFacesData(faces);
                        DisplayEmotionsData(faces);
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

        //Double Tapped Event for listview
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

        //Keyup enevt for listview
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

        /// <summary>
        /// Display Face Data
        /// </summary>
        /// <param name="result"></param>
        private void DisplayFacesData(Face[] faces, bool init = true)
        {
            if (faces == null)
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
            if (faces != null)
            {
                int count = 1;
                foreach (var face in faces)
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

            var list_child = gridFaces.Children.ToList();  
            list_child.ForEach((e) =>
            {
                if (e as TextBlock != null && (e as TextBlock).Tag != null)
                {
                    gridFaces.Children.Remove(e);
                }
            });

            int index = 1;
            foreach (var face in faces)
            {
                TextBlock txt0 = new TextBlock();
                txt0.Text = "0" + index;
                txt0.Padding = new Thickness(1);
                Grid.SetRow(txt0, index + 1);
                Grid.SetColumn(txt0, 0);
                txt0.Tag = true;

                TextBlock txt1 = new TextBlock();
                txt1.Text = Math.Round(face.FaceAttributes.Age, 2).ToString();
                txt1.Padding = new Thickness(1);
                Grid.SetRow(txt1, index + 1);
                Grid.SetColumn(txt1, 1);
                txt1.Tag = true;

                TextBlock txt2 = new TextBlock();
                txt2.Text = face.FaceAttributes.Gender;
                txt2.Padding = new Thickness(1);
                Grid.SetRow(txt2, index + 1);
                Grid.SetColumn(txt2, 2);
                txt2.Tag = true;

                TextBlock txt3 = new TextBlock();
                txt3.Text = Math.Round(face.FaceAttributes.Smile, 2).ToString();
                txt3.Padding = new Thickness(1);
                Grid.SetRow(txt3, index + 1);
                Grid.SetColumn(txt3, 3);
                txt3.Tag = true;

                TextBlock txt4 = new TextBlock();
                txt4.Text = face.FaceAttributes.Glasses.ToString();
                txt4.Padding = new Thickness(1);
                Grid.SetRow(txt4, index + 1);
                Grid.SetColumn(txt4, 4);
                txt4.Tag = true;

                index++;
                gridFaces.Children.Add(txt0);
                gridFaces.Children.Add(txt1);
                gridFaces.Children.Add(txt2);
                gridFaces.Children.Add(txt3);
                gridFaces.Children.Add(txt4);
            }
        }
        /// <summary>
        /// Display Emotions data
        /// </summary>
        /// <param name="emotions"></param>
        private void DisplayEmotionsData(Face[] faces, bool init = true)
        {
            if (faces == null)
                return;
            if (!init)
                return;

            var list_child = gridEmotions.Children.ToList();  
            list_child.ForEach((e) =>
            {
                if (e as TextBlock != null && (e as TextBlock).Tag != null)
                {
                    gridEmotions.Children.Remove(e);
                }
            });

            int index = 1;
            foreach (var face in faces)
            {
                TextBlock txt0 = new TextBlock();
                txt0.Padding = new Thickness(1);
                txt0.FontSize = 11;
                txt0.Text = "#" + index;
                Grid.SetRow(txt0, index + 1);
                Grid.SetColumn(txt0, 0);
                txt0.Tag = true;

                TextBlock txt1 = new TextBlock();
                txt1.Padding = new Thickness(1);
                txt1.FontSize = 11;
                txt1.Text = Math.Round(face.FaceAttributes.Emotion.Anger, 2).ToString();
                Grid.SetRow(txt1, index + 1);
                Grid.SetColumn(txt1, 1);
                txt1.Tag = true;

                TextBlock txt2 = new TextBlock();
                txt2.Padding = new Thickness(1);
                txt2.FontSize = 11;
                txt2.Text = Math.Round(face.FaceAttributes.Emotion.Contempt, 2).ToString();
                Grid.SetRow(txt2, index + 1);
                Grid.SetColumn(txt2, 2);
                txt2.Tag = true;

                TextBlock txt3 = new TextBlock();
                txt3.Padding = new Thickness(1);
                txt3.FontSize = 11;
                txt3.Text = Math.Round(face.FaceAttributes.Emotion.Disgust, 2).ToString();
                Grid.SetRow(txt3, index + 1);
                Grid.SetColumn(txt3, 3);
                txt3.Tag = true;

                TextBlock txt4 = new TextBlock();
                txt4.Padding = new Thickness(1);
                txt4.FontSize = 11;
                txt4.Text = Math.Round(face.FaceAttributes.Emotion.Fear, 2).ToString();
                Grid.SetRow(txt4, index + 1);
                Grid.SetColumn(txt4, 4);
                txt4.Tag = true;

                TextBlock txt5 = new TextBlock();
                txt5.Padding = new Thickness(1);
                txt5.FontSize = 11;
                txt5.Text = Math.Round(face.FaceAttributes.Emotion.Happiness, 2).ToString();
                Grid.SetRow(txt5, index + 1);
                Grid.SetColumn(txt5, 5);
                txt5.Tag = true;

                TextBlock txt6 = new TextBlock();
                txt6.Padding = new Thickness(1);
                txt6.FontSize = 11;
                txt6.Text = Math.Round(face.FaceAttributes.Emotion.Neutral, 2).ToString();
                Grid.SetRow(txt6, index + 1);
                Grid.SetColumn(txt6, 6);
                txt6.Tag = true;

                TextBlock txt7 = new TextBlock();
                txt7.Padding = new Thickness(1);
                txt7.FontSize = 11;
                txt7.Text = Math.Round(face.FaceAttributes.Emotion.Sadness, 2).ToString();
                Grid.SetRow(txt7, index + 1);
                Grid.SetColumn(txt7, 7);
                txt7.Tag = true;

                TextBlock txt8 = new TextBlock();
                txt8.Padding = new Thickness(1);
                txt8.FontSize = 11;
                txt8.Text = Math.Round(face.FaceAttributes.Emotion.Surprise, 2).ToString();
                Grid.SetRow(txt8, index + 1);
                Grid.SetColumn(txt8, 8);
                txt8.Tag = true;

                index++;
                gridEmotions.Children.Add(txt0);
                gridEmotions.Children.Add(txt1);
                gridEmotions.Children.Add(txt2);
                gridEmotions.Children.Add(txt3);
                gridEmotions.Children.Add(txt4);
                gridEmotions.Children.Add(txt5);
                gridEmotions.Children.Add(txt6);
                gridEmotions.Children.Add(txt7);
                gridEmotions.Children.Add(txt8);
            }
        }

        private async void imgPhoto_ImageOpened(object sender, RoutedEventArgs e)
        {
            size_image = new Size((imgPhoto.Source as BitmapImage).PixelWidth, (imgPhoto.Source as BitmapImage).PixelHeight);

            FaceServiceClient f_client = new FaceServiceClient(key_face);

            var requiedFaceAttributes = new FaceAttributeType[] {
                                FaceAttributeType.Age,
                                FaceAttributeType.Gender,
                                FaceAttributeType.Smile,
                                FaceAttributeType.FacialHair,
                                FaceAttributeType.HeadPose,
                                FaceAttributeType.Emotion,
                                FaceAttributeType.Glasses
                                };
            var faces_task = f_client.DetectAsync(txtLocation.Text, true, true, requiedFaceAttributes);

            faces = await faces_task;

            if (faces != null)
            {
                DisplayFacesData(faces);
                DisplayEmotionsData(faces);
            }

            ringLoading.IsActive = false;
        }
        /// <summary>
        /// Re-rendering Face Data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cvasMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DisplayFacesData(faces, false);
            DisplayEmotionsData(faces, false);
        }
        /// <summary>
        /// Display Emotion Data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cvasMain_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (faces != null)
            {
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
                foreach (var face in faces)
                {
                    Rect rect = new Rect();
                    rect.Width = face.FaceRectangle.Width * p;
                    rect.Height = face.FaceRectangle.Height * p;

                    rect.X = face.FaceRectangle.Left * p + offset_w;
                    rect.Y = face.FaceRectangle.Top * p + offset_h;

                    Point point = e.GetPosition(cvasMain);
                    if (rect.Contains(point))
                    {
                        EmotionDataControl edc = new EmotionDataControl();
                        var dic = new Dictionary<string, double>
                        {
                            {"Anger",face.FaceAttributes.Emotion.Anger },
                            {"Contempt",face.FaceAttributes.Emotion.Contempt },
                            {"Disgust",face.FaceAttributes.Emotion.Disgust },
                            {"Fear",face.FaceAttributes.Emotion.Fear },
                            {"Happiness",face.FaceAttributes.Emotion.Happiness },
                            {"Neutral",face.FaceAttributes.Emotion.Neutral },
                            {"Sadness",face.FaceAttributes.Emotion.Sadness },
                            {"Surprise",face.FaceAttributes.Emotion.Surprise },
                        };
                        edc.Data = dic;
                        edc.Width = cvasMain.ActualWidth * 3 / 4;
                        edc.Height = cvasMain.ActualHeight / 3;

                        emotionData.Child = edc;
                        emotionData.VerticalOffset = point.Y;
                        emotionData.HorizontalOffset = cvasMain.ActualWidth / 8;

                        emotionData.IsOpen = true;

                        break;
                    }
                }
            }
        }

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
    }
}
