using Keg.DAL;
using Keg.DAL.Models;
using Keg.UWP.Utils;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Keg.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //USE for TESTING ONLY
        private bool localTestOrientation = false;

        private DispatcherTimer timer;
        private DispatcherTimer refreshTimer;
        private CoreDispatcher MainPageDispatcher;

        /// <summary>
        /// Identifying the User Identity
        /// </summary>
        private string userGuid;


        private bool maintenance;

        //To Allow System to Holdon
        public bool Maintenance { 
            get
            {
                return maintenance;
            }
            set
            {
                maintenance = value;
                if (maintenance == true)
                {
                    //StartScanButton.Visibility = Visibility.Collapsed;
                    this.startScanTitle.Text = string.Format(Common.GetResourceText("Page1Maintenance"), string.Empty);
                }
                else
                {
                    //StartScanButton.Visibility = Visibility.Visible;
                }
            }
        }

        
        public MainPage()
        {
            this.InitializeComponent();
            
            //this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            this.DataContext = this;

            Debug.WriteLine("MainPage!!");

            MainPageDispatcher = Window.Current.Dispatcher;
           
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = TimeSpan.FromSeconds(1);

            refreshTimer = new DispatcherTimer();
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Interval = TimeSpan.FromHours(2); // Refresh every 2 hrs

            this.startScanTitle.Text = this["Page1Initiazing"];

            if (Common.KegSettings == null)
            {
                Task.Run(() => Common.GetKegSettings()).Wait();
            }

            //Get display Orientation
            var orient = Common.GetCurrentDisplaySize();
            Debug.WriteLine($"Display:{orient.Item1}, {orient.Item2}");
            Debug.WriteLine($"Window Bounds: Width-{Window.Current.Bounds.Width},Height-{Window.Current.Bounds.Height}");
            if (orient.Item2 == Windows.Graphics.Display.DisplayOrientations.Landscape ||
                orient.Item2 == Windows.Graphics.Display.DisplayOrientations.LandscapeFlipped)
            {
                Common.AppWindowWidth = orient.Item1.Width - 10;

            }
            else
            {
                Common.AppWindowWidth = orient.Item1.Width + 10;
            }
            
            /********  LOCAL TEST ********/
            if(localTestOrientation)
            {   
                //use one of below for local testing only
                //Portrait
                Common.AppWindowWidth = Window.Current.Bounds.Width + 20;

                //Landscape
                //Common.AppWindowWidth = Window.Current.Bounds.Width - 10;
            }

            TriggerOfVisualState.MinWindowWidth = Common.AppWindowWidth;
            //TriggerOfVisualStateBoth.MinWindowWidth = Common.AppWindowWidth;

            //Initialize 
            Initialize();

            this.Loaded += async (sender, e) =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    Window.Current.CoreWindow.KeyDown   += OnKeyDown;
                    App._temperature.TemperatureChanged += OnTemperatureChange;
                    App._weight.WeightChanged           += OnWeightChange;

                    UpdateDateTime();

                    timer.Start();
                    refreshTimer.Start();

                    this.startScanTitle.Text = this["Page1Initiazing"];

                    Debug.WriteLine("  Loaded!");
                });

                try
                {
                    await MainPageDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        bool valid = CheckKegStatus();

                        if(valid)
                        {
                            this.startScanTitle.Text = this["Page1BodyText"];
                        }

                    });
                }
                catch(Exception ex)
                {
                    this.startScanTitle.Text = ex.Message;
                    KegLogger.KegLogException(ex, "MainPage:Loaded", SeverityLevel.Critical);
                }
            };


            this.Unloaded += (sender, e) =>
            {
                timer.Stop();
                refreshTimer.Stop();
                App._temperature.TemperatureChanged -= OnTemperatureChange;
                App._weight.WeightChanged -= OnWeightChange;
            };

            KegLogger.KegLogTrace("Kegocnizer MainPage Loaded", "MainPageLoad", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information, null);

        }

        private async void RefreshTimer_Tick(object sender, object e)
        {
            await MainPageDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                this.startScanTitle.Text = this["Page1Maintenance"];

                Debug.WriteLine("Refresh Kick-off ...");
                Refresh();

            });
        }

        private async Task GetKegSettings()
        {
            await Common.GetKegSettings();
            Debug.WriteLine(" Refreshing Cloud settings ...");
            //Further Action 

        }

        private void Initialize()
        {
            if(Common.KegSettings == null)
            {
                this.startScanTitle.Text = this["GenericErrorPage"];
                return;
            }
            
            this.Maintenance = false;

            this.TemperatureText.Text = "--";
            this.PintsText.Text = "--";

            this.WarningBorderBrush.BorderBrush = App.Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;

            //SQLite Initalization
            SqLiteHelper.InitializeSqLiteDatabase();

            //Verify 
            KegLogger.KegLogTrace("Initialization complete.", "Initialize", SeverityLevel.Information, null);
        }

        private void Refresh()
        {
            Task.Run(() => GetKegSettings());

            bool valid = CheckKegStatus();
            if (valid)
            {
                this.startScanTitle.Text = this["Page1BodyText"];
            }

        }

        public string this[string key]
        {
            get
            {
                return Common.GetResourceText(key);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("  OnNavigated!");
            if (e.Parameter is string && !string.IsNullOrWhiteSpace((string)e.Parameter))
            {
                //Action
                if (e.Parameter.ToString().StartsWith("FromPage2:", StringComparison.CurrentCultureIgnoreCase))
                {
                    //TODO
                }


            }
        }

        

        private bool CheckKegStatus()
        {
            Debug.WriteLine(" Refreshing CheckKeg Status ...");

            this.WarningBorderBrush.Visibility = Visibility.Collapsed;
            this.warningTitle.Visibility = Visibility.Collapsed;

            this.PintsText.Style = App.Current.Resources["AppSubheaderTextBlockStyle"] as Style;

            //TODO: check all validations in Async

            bool valid = false;

            if (Common.KegSettings == null)
            {
                Task.Run(() => Common.GetKegSettings()).Wait();
            }

            //Maintenance Mode
            if(Common.KegSettings.MaintenanceMode)
            {
                this.Maintenance = true;
                return valid;
            }


            //Evalaute KegSettings

            //Core Hours:  Allowed Only if outside corehours
            //12T14;16T18

            //To decode entry to readable format
            StringBuilder entryToReadable = new StringBuilder();

            if (Common.KegSettings.CoreHours != null)
            {
                string[] coreHours = Common.KegSettings.CoreHours.Split(Common.semiColonDelimiter, StringSplitOptions.RemoveEmptyEntries);
                //Int32 currentHour = DateTime.Now;

                //Validation
                if (coreHours.Length == 0)
                    valid = false;

                
                foreach (var itemEntry in coreHours)
                {
                    //Split into
                    string[] entry = itemEntry.Split(Common.timeTDelimiter);
                    DateTime startTime = DateTime.Today;  //12AM
                    DateTime endTime = DateTime.Today.AddDays(1);    //11:59PM

                    if (entry.Length > 1)
                    {
                        if (entry[0].Trim().Length > 0)
                        {
                            int[] hrmin = entry[0].Split(Common.colonDelimiter).Select(x => int.Parse(x.ToString())).ToArray();

                            startTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, hrmin[0], (hrmin.Length > 1 ? hrmin[1] : 0), 0);
                        }
                        if (entry[1].Trim().Length > 0)
                        {
                            int[] hrmin = entry[1].Split(Common.colonDelimiter).Select(x => int.Parse(x.ToString())).ToArray();

                            endTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, hrmin[0], (hrmin.Length > 1 ? hrmin[1] : 0), 0);
                        }

                        entryToReadable.Append(startTime.ToString("hh:mm tt"));
                        entryToReadable.Append(" To ");
                        entryToReadable.Append(endTime.ToString("hh:mm tt"));
                        entryToReadable.Append(",");
                        //If multiple corehours are supplied, then all should be satisifed
                        valid = !(DateTime.Now.Ticks > startTime.Ticks && DateTime.Now.Ticks < endTime.Ticks);

                    }
                    else
                    {
                        //reject
                        valid = false;
                    }

                }
            }

            if(!valid)
            {
                //debugging purpose
                if (!App.IgnoreCoreHours)
                {
                    string coreHoursDisplay = entryToReadable.ToString();
                    if(coreHoursDisplay.EndsWith(","))
                    {
                        coreHoursDisplay = coreHoursDisplay.Substring(0, coreHoursDisplay.Length - 1);
                    }
                    this.startScanTitle.Text = string.Format(this["CoreHoursException"], coreHoursDisplay);
                    this.WarningBorderBrush.BorderBrush = App.Current.Resources["BorderBrush"] as SolidColorBrush;

                    return valid;
                }
            }

            //Validate CoreDays
            //ex: Mon,Tue, Wed, Thu,Fri
            string[] coreDays = Common.KegSettings.CoreDays.Split(Common.commaDelimiter, StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLowerInvariant().Trim()).ToArray();
            string charToday = DateTime.Today.ToString("ddd").ToLowerInvariant().Trim();

            if(coreDays.Contains(charToday))
            {
                valid = true;
            } else
            {
                //debugging purpose
                if (!App.IgnoreCoreHours)
                {
                    valid = false;
                    this.startScanTitle.Text = string.Format(this["CoreDaysException"], Common.KegSettings.CoreDays);
                    this.WarningBorderBrush.BorderBrush = App.Current.Resources["BorderBrush"] as SolidColorBrush;

                    return valid;
                    //throw new Exception(string.Format(Common.GetResourceText("CoreDaysException"), Common.KegSettings.CoreDays));
                }
            }

            //Avaialble Pints
            //TODO: 
            Measurement wtMeasurement = App._weight.GetWeight();
            if(wtMeasurement != null)
            {
                //float percentage = wtMeasurement.Amount * 10 / (Common.KegSettings.MaxKegWeight - Common.KegSettings.EmptyKegWeight);
                float percentage = wtMeasurement.Amount / (Common.KegSettings.MaxKegWeight - Common.KegSettings.EmptyKegWeight);
                Debug.WriteLine($"Percentage1: {percentage}");
                //TODO : Check the calibration
                if (percentage > 100.00f) 
                {
                    PintsText.Text = $"99% full";
                } else
                {
                    PintsText.Text = $"{Math.Round(percentage)}% full";
                }
                

                if(wtMeasurement.Amount < Common.MINIMUMLIMITTOEMPTY)
                {
                    //this.startScanTitle.Text = string.Format(Common.GetResourceText("Page1KegEmpty"));
                    this.warningTitle.Visibility = Visibility.Visible;
                    this.warningTitle.Text = string.Format(this["Page1KegEmpty"]);
                    this.WarningBorderBrush.BorderBrush = App.Current.Resources["BorderBrush"] as SolidColorBrush;

                    this.PintsText.Foreground = App.Current.Resources["BorderBrush"] as SolidColorBrush;

                    //DONT Stop user to proceed further as Weight can be not too accurate and helps user to get drink
                    //valid = false;
                    //return valid;
                }

            }
           
            try
            {
                Debug.WriteLine(" Refreshing LocalDB entries...");
                //Clean localDB 
                SqLiteHelper localDB = new SqLiteHelper();

                //Log Metrics
                //localDB.LogExpiredUserConsumption(Common.USERTIMEBASELINE);
                localDB.LogExpiredUserConsumption(Common.KegSettings.UserConsumptionReset);

                //Clean entries of user consumptions older than x minutes
                localDB.DeleteExpiredUserConsumption(Common.KegSettings.UserConsumptionReset);
            }
            catch(Exception ex)
            {
                KegLogger.KegLogException(ex, "MainPage:CheckKegStatus:DeleteExpiredUserConsumption", SeverityLevel.Error);
                    
                   // new dictionary<string, string>() {
                   //    {"userbaseline", common.usertimebaseline.tostring() }                       
                   //});
            }
            
            if (valid)
            {
                this.startScanTitle.Text = this["Page1BodyText"];
            }            

            return valid;
        }

        private void UpdateDateTime()
        {
            NativeTimeMethods.GetLocalTime(out SYSTEMTIME tTime);
            CurrentTime.Text = tTime.ToDateTime().ToString("G", CultureInfo.CurrentCulture);
        }

        #region Events

        private StringBuilder sb = new StringBuilder();

        public void OnKeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (e.VirtualKey == Windows.System.VirtualKey.Enter)
            {
                this.userGuid = Hasher.GetSmartCardHash(sb.ToString());
                Debug.WriteLine(userGuid);
                KegLogger.KegLogTrace("User", "UserScan", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information, new Dictionary<string, string>()
                {
                    {"hashcode", this.userGuid }
                });

                //resetting
                sb.Clear();
                bool status = CheckKegStatus();

                if (!status)
                {
                    //Appropriate messages are handled in above method
                    this.userGuid = string.Empty;
                    return;
                }

                Window.Current.CoreWindow.KeyDown -= OnKeyDown;
                //cardDetected = true;
                this.Frame.Navigate(typeof(Page2), $"FromMain:{userGuid}");
            } else
            {
                sb.Append((int)e.VirtualKey - 48);
            }
        }

        private bool tempeUnitsToggle = false;

        private async void OnTemperatureChange(object sender, MeasurementChangedEventArgs e)
        {
            await MainPageDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if(e.Measurement != null)
                {
                    //Debug.WriteLine($"{e.GetType().Name}: {e.Measurement}");

                    tempeUnitsToggle = !tempeUnitsToggle;
                    if (tempeUnitsToggle)
                    {
                        //Celsius
                        TemperatureText.Text = $"{ ((e.Measurement.Amount -32) * 5 / 9):N}° Celsius"; 
                       
                    } else
                    {
                        TemperatureText.Text = $"{e.Measurement.Amount:N}° {e.Measurement.Units}";
                    }

                    //Debug.WriteLine("Temp:" + TemperatureText.Text);

                    if (e.Measurement.Amount >= Common.MaxTempInsideKeg)
                    {
                        this.TemperatureText.Foreground = App.Current.Resources["BorderBrush"] as SolidColorBrush;
                    } else
                    {
                        this.TemperatureText.Foreground = App.Current.Resources["ForegroundColor"] as SolidColorBrush;
                    }
                }
                else
                {
                    TemperatureText.Text = $"-°-";
                }
              
            });
           
        }

        private async void OnWeightChange(object sender, MeasurementChangedEventArgs e)
        {
            Debug.WriteLine($"{e.GetType().Name}: {e.Measurement}");
            //1 pint = 16 Ounces
            //MaxKegVolume
            //62 = 100%
            //float percentage = e.Measurement.Amount*10 / (Common.KegSettings.MaxKegWeight - Common.KegSettings.EmptyKegWeight);
            float percentage = e.Measurement.Amount / (Common.KegSettings.MaxKegWeight - Common.KegSettings.EmptyKegWeight);
            Debug.WriteLine($"Percentage2: {percentage}");

            await MainPageDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                try
                {
                    this.warningTitle.Visibility = Visibility.Collapsed;
                    if (percentage > 100.00f)
                    {
                        PintsText.Text = $"99% full";
                    }
                    else
                    {
                        PintsText.Text = $"{ Math.Round(percentage)}% full";
                    }
                    
                    if (percentage < Common.MINIMUMLIMITTOEMPTY)
                    {
                        //this.startScanTitle.Text = string.Format(Common.GetResourceText("Page1KegEmpty"));
                        this.warningTitle.Visibility = Visibility.Visible;
                        this.warningTitle.Text = string.Format(this["Page1KegEmpty"]);
                        this.WarningBorderBrush.BorderBrush = App.Current.Resources["BorderBrush"] as SolidColorBrush;

                        this.PintsText.Foreground = App.Current.Resources["BorderBrush"] as SolidColorBrush;
                    }

                }
                catch
                {
                    PintsText.Text = $"--";
                }
            });

        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            //PlayBeep("beep-06.wav");
            
            //bool status = CheckKegStatus();

            //if(!status)
            //{
            //    //Appropriate messages are handled in above method
            //    return;
            //}
            ////TODO - Dummy 
            this.userGuid = "3050c65e-3a5b-475f-aa2f-734131ad0bbc";

            ////SqLiteHelper helper = new SqLiteHelper();
            ////helper.AddPersonConsumption(this.userGuid, 10);
            ////helper.AddPersonConsumption(this.userGuid, 20);

            ////helper.GetPersonConsumption(this.userGuid);
            this.Frame.Navigate(typeof(Page2), $"FromMain:{userGuid}");
        }

        private void Timer_Tick(object sender, object e)
        {
            UpdateDateTime();
        }


        private void SuccessScanButton_Click(object sender, RoutedEventArgs e)
        {
            ////Primary Validation
            ////a. Pints Available

            ////b. Temp under control
            ////c. System Status

            //this.Frame.Navigate(typeof(Page2), $"FromMain:{userGuid}");
        }


        #endregion Events

        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            ////Maintenance: Drop creates tables
            SqLiteHelper localDB = new SqLiteHelper();
            localDB.Clean();

            //Initialize();

            //CheckKegStatus();

        }

        private void Scan2Button_Click(object sender, RoutedEventArgs e)
        {
            ////TODO - Dummy 
            //this.userGuid = new Guid().ToString();

            ////SqLiteHelper helper = new SqLiteHelper();

            ////helper.AddPersonConsumption(this.userGuid, 10);
            ////helper.AddPersonConsumption(this.userGuid, 20);

            ////helper.GetPersonConsumption(this.userGuid);
            //this.Frame.Navigate(typeof(Page2), $"FromMain:{userGuid}");
        }

        private void Scan3Button_Click(object sender, RoutedEventArgs e)
        {
            //SqLiteHelper helper = new SqLiteHelper();
            //for (Int32 iCnt =0; iCnt<25;iCnt++)
            //{
            //    helper.AddPersonConsumption(Guid.NewGuid().ToString(), new Random().Next(1, 20));
            //}
            

            //helper.AddPersonConsumption(this.userGuid, 10);
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SP3_3.Width = Window.Current.Bounds.Width / 2;
            SP3_1.Width = Window.Current.Bounds.Width / 2;
            if (Window.Current.Bounds.Width >= Common.AppWindowWidth)
            {   //Landscape
                SP1.Width = Window.Current.Bounds.Width / 2;

            }
            else
            {
                //Portrait
                SP1.Width = Window.Current.Bounds.Width;
            }

        }
    }
}
