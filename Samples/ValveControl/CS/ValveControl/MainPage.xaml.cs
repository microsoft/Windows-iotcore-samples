using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Keg.DAL;
using System.Diagnostics;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ValveControl
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private FlowControl _flowControl { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

            _flowControl = new FlowControl();
            _flowControl.Initialize();
            _flowControl.FlowControlChanged += OnFlowControlChanged;

            FlowControlSwitch.Toggled += FlowControlSwitch_Toggled;
        }

        private void FlowControlSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            // turn on (or off) the valve via the relay
            _flowControl.IsActive = !_flowControl.IsActive;
        }

        private void OnFlowControlChanged(object sender, FlowControlChangedEventArgs e)
        {
            Debug.WriteLine("The valve was just " + (e.Flowing ? "opened" : "closed") + ".");
        }
    }
}
