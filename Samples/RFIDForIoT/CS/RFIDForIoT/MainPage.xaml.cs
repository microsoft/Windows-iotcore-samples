using System;
using Windows.UI.Xaml.Controls;
using RFIDForIoT.Models;
using Mfrc522Lib;
using System.Threading.Tasks;
using System.Diagnostics;
using PiezoBuzzerLib;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RFIDForIoT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IAsyncInitialization
    {
        public Mfrc522 rfidObj = new Mfrc522();
        public Buzzer piezo = new Buzzer();

        private Card _card;

        public Task Initialization { get; private set; }

        public MainPage()
        {
            this.InitializeComponent();
            Initialization = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await this.InitGPIO();
            this.piezo.InitPiezoGPIO();
            _card = new Card();
            await _card.InitializeAsync(1000, 1000, this.rfidObj);
            _card.CardDetected += new EventHandler<CardDetectedEventArgs>(async (s, e) => await OnCardDetected(s, e));
        }
        private async Task OnCardDetected(object sender, CardDetectedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                if (e.Card.SmartCardId != null)
                {
                    this.piezo.Buzz(this.piezo.pin);
                    OutputBox.Text = e.Card.SmartCardId;
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    OutputBox.Text = "";

                }
            });
            return;
        }
        private async Task InitGPIO()
        {
            await this.rfidObj.InitIO();
        }
    }


}
