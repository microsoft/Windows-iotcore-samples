using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Windows.Graphics.Imaging;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace CognitiveServicesExample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string _subscriptionKey = "<your subscription key?";
        private const string _serviceEndpoint = "<your endpoint, just protocol and base url>";

        private FaceClient faceServiceClient;

        BitmapImage bitMapImage;
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Uploads the image to be processed
        /// </summary>
        /// <param name="imageFilePath">The image file path.</param>
        /// <returns></returns>
        private async Task<DetectedFace[]> UploadAndDetectFaces(string url)
        {
            Debug.WriteLine("FaceClient is created");

            //
            // Create Face API Service client
            //
            faceServiceClient = new FaceClient(
                new ApiKeyServiceClientCredentials(_subscriptionKey),
                new System.Net.Http.DelegatingHandler[] { })
            {
                Endpoint = _serviceEndpoint
            };  // need to provide and endpoint and a delegate.


            // See https://docs.microsoft.com/en-us/azure/cognitive-services/face/glossary#a
            // for the current list of supported options.
            var requiredFaceAttributes = new FaceAttributeType[]
            {
                FaceAttributeType.Age,
                FaceAttributeType.Gender,
                FaceAttributeType.HeadPose,
                FaceAttributeType.Smile,
                FaceAttributeType.FacialHair,
                FaceAttributeType.Glasses,
                FaceAttributeType.Emotion,
                FaceAttributeType.Hair,
                FaceAttributeType.Makeup,
                FaceAttributeType.Occlusion,
                FaceAttributeType.Accessories,
                FaceAttributeType.Blur,
                FaceAttributeType.Exposure,
                FaceAttributeType.Noise
            };

            Debug.WriteLine("Calling Face.DetectWithUrlAsync()...");
            try
            {
                //
                // Detect the faces in the URL
                //

                var detectedFaces = await faceServiceClient.Face.DetectWithUrlAsync(url, true, false, requiredFaceAttributes);

                return detectedFaces.ToArray();
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Detection failed. Please make sure that you have the right subscription key and proper URL to detect.");
                Debug.WriteLine(exception.ToString());
                return null;
            }
        }

        private async void Button_Clicked(object sender, RoutedEventArgs e)
        {
            ImageCanvas.Children.Clear();
            ResultBox.Items.Clear();

            string urlString = ImageURL.Text;
            Uri uri;
            try
            {
                uri = new Uri(urlString, UriKind.Absolute);
            }
            catch (UriFormatException ex)
            {
                Debug.WriteLine(ex.Message);

                var dialog = new MessageDialog("URL is not correct");

                await dialog.ShowAsync();

                return;
            }

            //Load image from URL
            bitMapImage = new BitmapImage
            {
                UriSource = uri
            };

            ImageBrush imageBrush = new ImageBrush
            {
                ImageSource = bitMapImage
            };

            //Load image to UI
            ImageCanvas.Background = imageBrush;

            detectionStatus.Text = "Detecting...";

            //urlString = "http://blogs.cdc.gov/genomics/files/2015/11/ThinkstockPhotos-177826416.jpg"

            DetectedFace[] detectedFaces = await UploadAndDetectFaces(urlString);


            if (detectedFaces != null)
            {
                DisplayParsedResults(detectedFaces);
                DisplayAllResults(detectedFaces);
                DrawFaceRectangle(detectedFaces, bitMapImage, urlString);

                detectionStatus.Text = "Detection Done";
            }
            else
            {
                detectionStatus.Text = "Detection Failed";
            }
        }

        private void DisplayAllResults(DetectedFace[] faceList)
        {
            int index = 0;
            foreach (DetectedFace face in faceList)
            {
                var emotion = face.FaceAttributes.Emotion;

                ResultBox.Items.Add("\nFace #" + index
                    + "\nAnger: " + emotion.Anger
                    + "\nContempt: " + emotion.Contempt
                    + "\nDisgust: " + emotion.Disgust
                    + "\nFear: " + emotion.Fear
                    + "\nHappiness: " + emotion.Happiness
                    + "\nNeutral: " + emotion.Neutral
                    + "\nSadness: " + emotion.Sadness
                    + "\nSurprise: " + emotion.Surprise);

                index++;
            }
        }

        private void DisplayParsedResults(DetectedFace[] resultList)
        {
            int index = 0;
            string textToDisplay = "";

            foreach (DetectedFace face in resultList)
            {
                string emotionString = ParseResults(face);
                textToDisplay += "Person number " + index.ToString() + " " + emotionString + "\n";
                index++;
            }
            ResultBox.Items.Add(textToDisplay);
        }

        private string ParseResults(DetectedFace face)
        {
            double topScore = 0.0d;
            string topEmotion = "";
            string retString = "";
            var emotion = face.FaceAttributes.Emotion;

            // anger
            topScore = face.FaceAttributes.Emotion.Anger;
            topEmotion = "Anger";

            // contempt
            if (topScore < emotion.Contempt)
            {
                topScore = emotion.Contempt;
                topEmotion = "Contempt";
            }

            // disgust
            if (topScore < emotion.Disgust)
            {
                topScore = emotion.Disgust;
                topEmotion = "Disgust";
            }

            // fear
            if (topScore < emotion.Fear)
            {
                topScore = emotion.Fear;
                topEmotion = "Fear";
            }

            // happiness
            if (topScore < emotion.Happiness)
            {
                topScore = emotion.Happiness;
                topEmotion = "Happiness";
            }

            // neural
            if (topScore < emotion.Neutral)
            {
                topScore = emotion.Neutral;
                topEmotion = "Neutral";
            }

            // happiness
            if (topScore < emotion.Sadness)
            {
                topScore = emotion.Sadness;
                topEmotion = "Sadness";
            }

            // surprise
            if (topScore < emotion.Surprise)
            {
                topScore = emotion.Surprise;
                topEmotion = "Surprise";
            }

            retString = $"is expressing {topEmotion} with a certainty of {topScore}.";
            return retString;
        }


        public async void DrawFaceRectangle(DetectedFace[] faceResult, BitmapImage bitMapImage, String urlString)
        {


            if (faceResult != null && faceResult.Length > 0)
            {
                Windows.Storage.Streams.IRandomAccessStream stream = await Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(new Uri(urlString)).OpenReadAsync();


                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);


                double resizeFactorH = ImageCanvas.Height / decoder.PixelHeight;
                double resizeFactorW = ImageCanvas.Width / decoder.PixelWidth;


                foreach (var face in faceResult)
                {

                    FaceRectangle faceRect = face.FaceRectangle;

                    Image Img = new Image();
                    BitmapImage BitImg = new BitmapImage();
                    // open the rectangle image, this will be our face box
                    Windows.Storage.Streams.IRandomAccessStream box = await Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/rectangle.png", UriKind.Absolute)).OpenReadAsync();

                    BitImg.SetSource(box);

                    //rescale each facebox based on the API's face rectangle
                    var maxWidth = faceRect.Width * resizeFactorW;
                    var maxHeight = faceRect.Height * resizeFactorH;

                    var origHeight = BitImg.PixelHeight;
                    var origWidth = BitImg.PixelWidth;


                    var ratioX = maxWidth / (float)origWidth;
                    var ratioY = maxHeight / (float)origHeight;
                    var ratio = Math.Min(ratioX, ratioY);
                    var newHeight = (int)(origHeight * ratio);
                    var newWidth = (int)(origWidth * ratio);

                    BitImg.DecodePixelWidth = newWidth;
                    BitImg.DecodePixelHeight = newHeight;

                    // set the starting x and y coordiantes for each face box
                    Thickness margin = Img.Margin;

                    margin.Left = faceRect.Left * resizeFactorW;
                    margin.Top = faceRect.Top * resizeFactorH;

                    Img.Margin = margin;

                    Img.Source = BitImg;
                    ImageCanvas.Children.Add(Img);

                }

            }
        }
    }
}
