using Keg.DAL;
using Keg.DAL.Models;
using Keg.UWP.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;
using Windows.System.UserProfile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.ApplicationInsights.DataContracts;

namespace Keg.UWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        //public static ResourceLoader resourceLoader;
        //public Temperature _temperature { get; set; }

        public static readonly bool IgnoreCoreHours = false;

        internal static Dictionary<string, object> calibration;

        public static Temperature _temperature;
        public static Weight _weight;
        public static Flow _flow;
        public static FlowControl _flowControl;

        //public static TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;


            
            try
            {
                Task.Run(() => Common.GetKegSettings()).Wait();
            }
            catch(Exception ex)
            {
                //Azure Down
                //TODO
                Debug.WriteLine($"Exception:{ex.Message}");

                KegLogger.KegLogException(ex, "App:App", SeverityLevel.Critical);
                throw ex;
            }


            InitializeCallibrations();

        }
        
        internal void InitializeCallibrations()
        {
            calibration = new Dictionary<string, object>();
            try
            {
                // to initialize a sensor measurement object, we pass it a calibration object
                // eventually, these will come from a specific Cosmos document that the app will passthru
                foreach (var c in Weight.GetDefaultCalibrationSettings()) calibration[c.Key] = c.Value;
                foreach (var c in FlowControl.GetDefaultCalibrationSettings()) calibration[c.Key] = c.Value;
                foreach (var c in Temperature.GetDefaultCalibrationSettings()) calibration[c.Key] = c.Value;
                foreach (var c in Flow.GetDefaultCalibrationSettings()) calibration[c.Key] = c.Value;

                calibration[Weight.AdjustWeightFactorSetting] = Common.KegSettings.WeightCalibrationFactor.ToString();
                calibration[Weight.AdjustWeightOffsetSetting] = Common.KegSettings.WeightCalibrationOffset.ToString();

                if (calibration.ContainsKey(Temperature.AdjustTemperatureSetting))
                {
                    calibration[Temperature.AdjustTemperatureSetting] = new Measurement(-2.0f, Measurement.UnitsOfMeasure.Fahrenheit);
                }
                else
                {
                    calibration.Add(Temperature.AdjustTemperatureSetting, new Measurement(-2.0f, Measurement.UnitsOfMeasure.Fahrenheit));
                }

                //Flow Calibration
                calibration[Flow.FlowCalibrationFactorSetting] = Common.KegSettings.FlowCalibrationFactor.ToString();
                calibration[Flow.FlowCalibrationOffsetSetting] = Common.KegSettings.FlowCalibrationOffset.ToString();

                App._flowControl = new FlowControl(App.calibration);
                App._flowControl.Initialize(1000, 1000);

                App._flow = new Flow(App.calibration);
                App._flow.Initialize(1000, 1000);

                //Objects Initializations
                App._temperature = new Temperature(App.calibration);
                //App._temperature.TemperatureChanged += OnTemperatureChange;
                App._temperature.Initialize(1000, 10000);

                App._weight = new Weight(App.calibration);
                //App._weight.WeightChanged += OnWeightChange;
                App._weight.Initialize();
                //App._weight.Initialize(1000, 50000);
                App._weight.Initialize(1000, 10000);

                KegLogger.KegLogTrace("Kegocnizer App Loaded", "AppLoad", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information, null);
            }
            catch (Exception ex)
            {
                Dictionary<string, string> log = new Dictionary<string, string>();
                foreach (var item in calibration)
                {
                    log.Add(item.Key, item.Value.ToString());
                }
                KegLogger.KegLogTrace(ex.Message, "App:InitializeCallibrations", SeverityLevel.Critical, log);

                KegLogger.KegLogException(ex, "App:InitializeCallibrations", SeverityLevel.Critical);
#if !DEBUG
                throw;
#endif
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                
                //Set Default Primary Language
                //Setting this, will be directly reflected in ApplicationLanguages.Languages 
                ApplicationLanguages.PrimaryLanguageOverride = GlobalizationPreferences.Languages[0];
                // Set the default language
                rootFrame.Language = "en-US";

                //resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

                // Refresh the resources in new language
                ResourceContext.GetForCurrentView().Reset();
                ResourceContext.GetForViewIndependentUse().Reset();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            //Exception ex = new Exception("Failed to load Page " + e.SourcePageType.FullName);
            KegLogger.KegLogException(e.Exception, "App:OnNavigationFailed", SeverityLevel.Critical);

            KegLogger.KegLogTrace(e.Exception.Message, "App:OnNavigationFailed", SeverityLevel.Critical,
                new Dictionary<string, string>() {
                       {"SourcePage", e.SourcePageType.FullName }
                   });
            throw e.Exception;
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

    }
        
}
