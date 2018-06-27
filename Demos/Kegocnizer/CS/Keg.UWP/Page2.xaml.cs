using Keg.DAL;
using Keg.DAL.Models;
using Keg.UWP.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.ApplicationInsights.DataContracts;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Keg.UWP
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Page2 : Page
    {
        private CoreDispatcher Page2Dispatcher;

        private User            loggedInUser;
        private DispatcherTimer timer;
        private DispatcherTimer countdown;
        
        private bool            deliverOunces;

        private List<float>     dispensed;
        private double          totalConsumption;
        private bool            imageLoaded;

        private int             counter;
        private int Counter {
            get
            {
                return this.counter;
            }
            set
            {
                this.counter = value;
                this.CounterText.Text = this.counter.ToString();
            }
        }
        
        public Page2()
        {
            this.InitializeComponent();

            
            Page2Dispatcher = Window.Current.Dispatcher;

            //remove
            var orient = Common.GetCurrentDisplaySize();
            Debug.WriteLine($"Page2 Display:{orient.Item1}, {orient.Item2}");
            Debug.WriteLine($"Window Bounds: Width-{Window.Current.Bounds.Width},Height-{Window.Current.Bounds.Height}");
            
            TriggerOfVisualState.MinWindowWidth = Common.AppWindowWidth;

            deliverOunces = false;
            totalConsumption = 0.0f;

            this.Page2ScreenTimeout.Text = Common.GetResourceText("Page2ScreenTimeout");
            //Get the User Limits+

            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = TimeSpan.FromSeconds(15);//Initial wait

            countdown = new DispatcherTimer();
            countdown.Tick += Countdown_Tick;
            countdown.Interval = TimeSpan.FromSeconds(1);

            this.Loaded += async (sender, e) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    //timer.Start();
                    InitializeLayout();

                });
            };
            
            Unloaded += Page2_Unloaded;
            this.KegTitle.Text = Common.GetResourceText("KegTitle");

            KegLogger.KegLogTrace("Kegocnizer Page2 Loaded", "Page2Load", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information, null);
        }

        private void Page2_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            countdown.Stop();
            Counter = 0;            
            StandardPopup.IsOpen = false;

            if(App._flowControl != null)
            {
                App._flowControl.Dispose();
                App._flowControl.FlowControlChanged -= OnFlowControlChanged;
            }

            if (App._flow != null)
            {
                App._flow.Dispose();
                App._flow.FlowChanged -= OnFlowChange;

            }
            
            this.dispensed = null;
            
            
        }

        private void BackButton_Clicked(object sender, RoutedEventArgs e)
        {
            //UpdateUserConsumption();

            //this.Frame.Navigate(typeof(MainPage),"FromPage2:");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.GridConsumption.Visibility = Visibility.Collapsed;
            this.Page2OuncesPanel.Visibility = Visibility.Collapsed;

            if (e.Parameter is string && !string.IsNullOrWhiteSpace( (string)e.Parameter))
            {
                //Action
                if(e.Parameter.ToString().StartsWith("FromMain:", StringComparison.CurrentCultureIgnoreCase) )
                {
                    //Validating
                    this.Page2Part1Text.Text = Common.GetResourceText("Page2ValidationText");
                    this.Page2Part2Text.Text = string.Empty;
                    this.Page2Image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/icons8-beer-50.png", UriKind.RelativeOrAbsolute));

                    //TODO: Add Check and validation
                    StartAction(e.Parameter.ToString().Split( new char[] { ':'})[1] );

                }

            }
            else
            {
                //Action
                this.Frame.Navigate(typeof(MainPage));
            }
            base.OnNavigatedTo(e);  

        }

        private async void StartAction(string userId)
        {
            StandardPopup.IsOpen = false;
            lastMeasurement = 0.0f;

            string beepFileName = "beep-06.wav";

            //Start Validation
            if (Common.KegSettings == null)
                await Common.GetKegSettings();

            //User Validation
            await Page2Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
            {
                if(Window.Current.Bounds.Width >= Common.AppWindowWidth)
                {
                    this.Page2LimitText1.Text = $"{Common.KegSettings.MaxUserOuncesPerHour.ToString()} Oz.(Max)";
                    this.Page2LimitText3.Text = "0 Oz.(Min)";

                } else
                {
                    this.Page2LimitText3.Text = $"{Common.KegSettings.MaxUserOuncesPerHour.ToString()} Oz.(Max)";
                    this.Page2LimitText1.Text = "0 Oz.(Min)";
                }
                

                User user = await User.GetUserByHashcode(userId);
                this.loggedInUser = user;

                Dictionary<string, string> props = new Dictionary<string, string>
                {
                    { "UserID", user != null ? user.HashCode : string.Empty }
                };

                KegLogger.KegLogEvent("User Badge Scan", "BadgeScan", props);

                if (null != this.loggedInUser && this.loggedInUser.Type == "KegUser")
                {
                    //Check Database
                    SqLiteHelper localDB = new SqLiteHelper();

                    //Check if 25 people limit reached
                    //Check Event serve Count
                    Int32 visited = localDB.GetVisitedPersonsCount(Common.KegSettings.MaxEventDurationMinutes);

                    if (visited >= Common.KegSettings.MaxPersonsPerEvent)
                    {
                        this.Page2Part1Text.Text = Common.GetResourceText("Page2ServeLimitText");

                        List<string> visitors = localDB.GetVisitedPersons(Common.KegSettings.MaxEventDurationMinutes).ToList();
                        if(visitors.Any( v => v.Equals( this.loggedInUser.HashCode, StringComparison.InvariantCultureIgnoreCase))) {
                            
                            //Found in list and this particular user is allowed

                        } else
                        {
                            //Not found, no new users accepted
                            this.Page2Part1Text.Text = Common.GetResourceText("Page2ServeLimitText");
                            this.Page2Image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/no-beer.png"));

                            beepFileName = "fail-buzzer-04.wav";

                            Counter = Common.COUNTERSHORTWAIT;
                            timer.Start();

                            //Raise event on max count reached
                            KegLogger.KegLogEvent("Max Visitors reached!", "MaxVisitors", null);

                            return;
                        }
                    }

                    //Check total consumption
                    totalConsumption = localDB.GetPersonConsumption(userId);
                    if (totalConsumption >= Common.KegSettings.MaxUserOuncesPerHour)
                    {
                        // If Limit Reached, display required text
                        this.Page2Part1Text.Text = Common.GetResourceText("Page2LimitSorryText");
                        this.Page2Part2Text.Text = Common.GetResourceText("Page2LimitReachedText");
                        this.Page2Image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/no-beer.png"));

                        beepFileName = "fail-buzzer-04.wav";

                        //TODO:
                        //Start a timer to return to main screen 
                        Counter = Common.COUNTERSHORTWAIT;
                        timer.Start();
                        
                    }
                    else
                    {
                        //Success:
                        this.Page2Part1Text.Text = Common.GetResourceText("Page2SuccessValidationText");
                        this.Page2Part2Text.Text = Common.GetResourceText("Page2SuccessStart");
                        this.Page2Image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/checkmark.png"));

                        this.GridConsumption.Visibility = Visibility.Visible;
                        this.Page2OuncesPanel.Visibility = Visibility.Visible;
                        
                        this.Page2LimitText2.Text = $"{totalConsumption.ToString()} Oz";

                        AllowedLimitFill();
                        
                        deliverOunces = true;
                        dispensed = new List<float>();


                        //Initialize
                        if (App._flowControl != null)
                        {
                            //App._flowControl = new FlowControl(App.calibration);
                            App._flowControl.FlowControlChanged += OnFlowControlChanged;
                            // App._flowControl.Initialize(1000, 1000);

                            App._flowControl.IsActive = true;
                            Reset(true);
                        }

                        if (App._flow != null)
                        {
                            //App._flow = new Flow(App.calibration);
                            App._flow.FlowChanged += OnFlowChange;
                            //App._flow.Initialize();
                            //App._flow.Initialize(1000, 1000);

                            App._flow.ResetFlow();

                        }

                        //Initialize Flow measure
                        dispensed = new List<float>();

                        Counter = Common.COUNTERWAIT;
                        timer.Start();
                    }
                }
                else
                {
                    ////Access Denied:
                    this.Page2Part1Text.Text = Common.GetResourceText("Page2SorryText");
                    this.Page2Part2Text.Text = Common.GetResourceText("Page2ContactText");
                    this.Page2Image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/no-beer.png"));

                    beepFileName = "fail-buzzer-04.wav";
                    //TODO:
                    //Start a timer to return to main screen 
                    Counter = Common.COUNTERSHORTWAIT;
                    timer.Start();

                    //denied users
                    KegLogger.KegLogEvent("User Denied!", "UserDenied", new Dictionary<string, string>()
                    {
                        { "UserID", userId }
                    } );
                }

                PlayBeep(beepFileName);

            });
            
        }

        private async void PlayBeep(String fileName)
        {
            //ms-appx:///Assets/media/beep-06.wav
            await Page2Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
            {
                MediaElement playbackMediaElement = new MediaElement();
                StorageFolder appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFolder assets = await appInstalledFolder.GetFolderAsync("Assets\\media");
                var files = await assets.GetFilesAsync();
                StorageFile storageFile = files.FirstOrDefault(a => a.Name == fileName);

                //foreach (StorageFile storageFile in files)
                {

                    using (IRandomAccessStream fileStream = await storageFile.OpenAsync(FileAccessMode.Read))
                    {
                        /// IRandomAccessStream stream = await storageFile.OpenAsync(FileAccessMode.Read);
                        playbackMediaElement.SetSource(fileStream.CloneStream(), storageFile.FileType);
                        playbackMediaElement.Play();

                    }
                }

            });
        }


        /// <summary>
        /// Draw Gradient Allowed Limit 
        /// </summary>
        /// <param name="consumed"> Extra Ounces to add to currently consumed recorded</param>
        private async void AllowedLimitFill(double consumed =0 )
        {
            await Page2Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                this.Page2LimitText2.Visibility = Visibility.Collapsed;

                double percentage = (totalConsumption + consumed) / Common.KegSettings.MaxUserOuncesPerHour;

                var w = 1 - percentage;

                List<Color> colors = new List<Color>
                {
                    (App.Current.Resources["ProgressBarBackground"] as SolidColorBrush).Color,
                    (App.Current.Resources["ProgressBarForeground"] as SolidColorBrush).Color
                };

                var brush = new LinearGradientBrush()
                {
                    StartPoint = new Point(1, 0.5),
                    EndPoint = new Point(0, 0.5)
                };
                
                if (Window.Current.Bounds.Width >= Common.AppWindowWidth)
                {
                    brush = new LinearGradientBrush()
                    {
                        StartPoint = new Point(0.5, 0),
                        EndPoint = new Point(0.5, 1)
                    };

                    //TODO:
                    RPPage2OuncesPanel.Height = this.SP1.ActualHeight;
                    Page2LimitText2.Padding = new Thickness(20, 0, 0, this.AllowedLimitBar.RenderSize.Height * percentage *0.85);

                }
                else
                {
                    //TODO:
                    //RPPage2OuncesPanel.Height = 40;//this.GridConsumption.ActualHeight +10;
                    Page2LimitText2.Margin = new Thickness(this.AllowedLimitBar.RenderSize.Width * percentage *0.85, 20, 0, 0);
                }

                brush.GradientStops.Add(new GradientStop()
                {
                    Color = colors[0],
                    Offset = w
                });
                brush.GradientStops.Add(new GradientStop()
                {
                    Color = colors[1],
                    Offset = w
                });

                //AllowedLimitBar.Margin = new Thickness(5);
               
                this.AllowedLimitBar.Fill = brush;
                
                this.Page2LimitText2.Text = $"{ Math.Round(totalConsumption + consumed,2, MidpointRounding.ToEven) } Oz.";
                this.Page2LimitText2.Visibility = Visibility.Visible;

            });

        }
        
        private async void ShowPopupCounter()
        {
            await Page2Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                CoreWindow currentWindow = Window.Current.CoreWindow;
                StandardPopup.VerticalOffset = (currentWindow.Bounds.Height / 2) - (gdChildStackPanel.Height / 2);
                StandardPopup.HorizontalOffset = (currentWindow.Bounds.Width / 2) - (gdChildStackPanel.Width / 2);
                StandardPopup.IsOpen = true;

            });
        }

        private void HidePopupCounter()
        {
            Reset(false);

        }

        private async void UpdateUserConsumption()
        {
            this.imageLoaded = false;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (null != this.loggedInUser && deliverOunces)
                {

                    if (App._flow != null )
                    {
                        Measurement desp = App._flow.GetFlow();
                        if (desp != null)
                        {
                            dispensed.Add(desp.Amount);
                        }
                    }


                    //Disable FlowControl
                    if (App._flowControl != null)
                    {
                        App._flowControl.IsActive = false;
                        Reset(false);
                    }

                    //Resetting dispensed collection to 0
                    if (App._flow != null) App._flow.ResetFlow();

                    float totalDispensed = dispensed != null ? dispensed.Sum() : 0.0f;

                    if (totalDispensed > 0.0f)
                    {
                        SqLiteHelper localDB = new SqLiteHelper();
                        //TODO: Dummy code of random value
                        //localDB.AddPersonConsumption(this.loggedInUser.HashCode, new Random().Next(1, 20));
                        localDB.AddPersonConsumption(this.loggedInUser.HashCode, totalDispensed);

                        KegLogger.KegLogEvent("Beer Delivered!", "Delivered", new Dictionary<string, string>()
                        {
                            { "UserID", this.loggedInUser.HashCode },
                            { "Quantity", totalDispensed.ToString()}
                        });

                    }

                }

                this.loggedInUser = null;
                this.Frame.Navigate(typeof(MainPage), $"FromPage2:");
            });

        }

        private void Reset(bool on = true)
        {
            this.StandardPopup.IsOpen = false;
            countdown.Stop();
            imageLoaded = false;

            if (on)
            {
                timer.Stop();
                Debug.WriteLine("Reset:Timer.Stop");
                //timer.Interval = new TimeSpan(0, 0, 0, 15);
                this.CounterText.Text = Common.COUNTERWAIT.ToString();
            }
            else
            {
                timer.Start();
                this.Page2Image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/beer.png"));
                Debug.WriteLine("Reset:Timer.Start");
            }
        }

        private void InitializeLayout()
        {
            AllowedLimitBar.Margin = new Thickness { Bottom = 10, Top = 10, Left = 10, Right = 10 };

            if (Window.Current.Bounds.Width >= Common.AppWindowWidth)
            {
                //Landscape
                SP1.Width = Window.Current.Bounds.Width * 2 / 3;
                GridConsumption.Width = 30;  //Window.Current.Bounds.Width / 9;
                Page2OuncesPanel.Width = (Window.Current.Bounds.Width / 3)-30;

                Debug.WriteLine("SP1:" + this.SP1.ActualHeight);
                Debug.WriteLine("Grid:" + this.GridConsumption.ActualHeight);
                Debug.WriteLine("Bar:" + this.AllowedLimitBar.RenderSize.Height);

                this.Page2LimitText1.Text = $"{Common.KegSettings.MaxUserOuncesPerHour.ToString()} Oz.(Max)";
                this.Page2LimitText3.Text = "0 Oz.(Min)";

                AllowedLimitBar.HorizontalAlignment = HorizontalAlignment.Right;
                AllowedLimitBar.Width = Page2OuncesPanel.ActualWidth / 3;
            }
            else
            {
                SP1.Width = Window.Current.Bounds.Width;
                Page2OuncesPanel.Width = Window.Current.Bounds.Width;
                GridConsumption.Width = Window.Current.Bounds.Width;

                RPPage2OuncesPanel.Width = Window.Current.Bounds.Width;

                this.Page2LimitText3.Text = $"{Common.KegSettings.MaxUserOuncesPerHour.ToString()} Oz.(Max)";
                this.Page2LimitText1.Text = "0 Oz.(Min)";

            }

            AllowedLimitFill();

        }

        internal async void OnFlowControlChanged(object sender, FlowControlChangedEventArgs e)
        {
            await Page2Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                try
                {
                    Debug.WriteLine($"{e.GetType().Name}: {e.Flowing}");
                }
                catch(Exception ex)
                {
                    KegLogger.KegLogException(ex, "Page2:OnFlowConrolChanged", SeverityLevel.Warning);
                   //     new Dictionary<string, string>() {
                   //    {"Flowing", e.Flowing.ToString() }
                   //});
                }
            });

        }

        private float lastMeasurement = 0.0f;

        internal async void OnFlowChange(object sender, MeasurementChangedEventArgs e)
        {
          
            await Page2Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                try
                {
                    Debug.WriteLine($"Prior: {e.GetType().Name}: {e.Measurement}");

                    if (e.Measurement != null && e.Measurement.Amount > 0.0f && e.Measurement.Amount > lastMeasurement)
                    {
                        Debug.WriteLine($"****: {e.GetType().Name}: {e.Measurement} ****");
                        lastMeasurement = e.Measurement.Amount;

                        Counter = Common.COUNTERSHORTWAIT;

                        if(!timer.IsEnabled)
                        {
                            Reset(false);
                        } else
                        {
                            timer.Stop();
                            timer.Start();
                        }
                        
                        //HidePopupCounter();

                        AllowedLimitFill(e.Measurement.Amount);

                        //Check user max limit
                        if( (totalConsumption + e.Measurement.Amount) >= Common.KegSettings.MaxUserOuncesPerHour)
                        {
                            //Cut-off user
                            App._flowControl.IsActive = false;

                            // If Limit Reached, display required text
                            this.Page2Part1Text.Text = Common.GetResourceText("Page2LimitSorryText");
                            this.Page2Part2Text.Text = Common.GetResourceText("Page2LimitReachedText");
                            this.Page2Image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/no-beer.png"));
 
                            //TODO:
                            //Better to show message in popup about why user need to exit
                            //String: Page2LimitReachedText

                        }
                        else
                        {
                            if (!imageLoaded)
                            {
                                //Start Pouring:
                                this.Page2Part1Text.Text = Common.GetResourceText("Page2SuccessValidationText");
                                this.Page2Part2Text.Text = Common.GetResourceText("Page2SuccessStart");
                                this.Page2Image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/beer.gif"));
                                imageLoaded = true;
                            }
                        }
                    }
                    
                }
                catch(Exception ex)
                {
                    KegLogger.KegLogException(ex, "Page2:OnFlowChanged", SeverityLevel.Error);

                    KegLogger.KegLogTrace(ex.Message, "Page2:OnFlowChanged", SeverityLevel.Error,
                        new Dictionary<string, string>() {
                       {"Measurement", e.Measurement !=null ? e.Measurement.Amount.ToString(): string.Empty }
                   });
                }
            });

        }

        private void Countdown_Tick(object sender, object e)
        {
            string value = this.CounterText.Text;

            //var value = LogoutProgress.Value - LogoutProgress.SmallChange * 5;
            //LogoutProgress.Value = value;
            if (Int32.Parse(value) <=0 )
            {
                Debug.WriteLine($"****: CountDown Ended...closing...");

                countdown.Stop();
                this.StandardPopup.IsOpen = false;
                UpdateUserConsumption();
            }
            else
            {
                this.CounterText.Text = (Int32.Parse(value) - 1).ToString();
                
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            timer.Stop();
            //timer.Interval = TimeSpan.FromSeconds(15);

            this.CounterText.Text = counter.ToString(); //Common.COUNTERWAIT.ToString();
            ShowPopupCounter();
            countdown.Start();
        }

        private async void FillButton_Click(object sender, RoutedEventArgs e)
        {
            await Page2Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                double percentage = (totalConsumption + 12.3) / Common.KegSettings.MaxUserOuncesPerHour;

                AllowedLimitFill(12.3);
            });

            //Reset
            //PauseButton_Click(sender, e);
            Reset(true);


            //Start Pouring:
            this.Page2Part1Text.Text = Common.GetResourceText("Page2SuccessValidationText");
            this.Page2Part2Text.Text = Common.GetResourceText("Page2SuccessStart");
            this.Page2Image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/beer.gif"));

            //On the Flow Control Relay
            App._flowControl.IsActive = true;

        }


        private void MainButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateUserConsumption();
        }
        
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            //User re-started pouring
            //reset
            Reset(false);           

        }

        // Handles the Click event on the Button on the page and opens the Popup. 
        private void ShowPopupOffsetClicked(object sender, RoutedEventArgs e)
        {
            // open the Popup if it isn't open already 
            if (!StandardPopup.IsOpen) { StandardPopup.IsOpen = true; }
        }

       
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InitializeLayout();
        }
        
    }
}
