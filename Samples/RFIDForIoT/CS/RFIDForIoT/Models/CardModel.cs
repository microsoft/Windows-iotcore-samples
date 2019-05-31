using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mfrc522Lib;

namespace RFIDForIoT.Models
{
    public class Card
    {
        public event EventHandler<CardDetectedEventArgs> CardDetected;
        public Timer _timer;

        public async Task InitializeAsync(int initialDelay, int period, Mfrc522 obj)
        {
            _timer = new Timer(OnTimerAsync, obj, initialDelay, period);
        }

        public async void OnTimerAsync(object state)
        {
            OnCardDetected(new CardDetectedEventArgs(await GetCard((Mfrc522)state)));
        }

        public static async Task<CardModel> GetCard(Mfrc522 mfrc)
        {
            CardModel card = await CardModel.BuildCardModelAsync(mfrc);
            return card;
        }

        protected void OnCardDetected(CardDetectedEventArgs cardArgs)
        {
            CardDetected?.Invoke(this, cardArgs);
        }
    }

    public class CardModel
    {
        public string SmartCardId { get; set; }

        public CardModel(String smartcardid)
        {
            SmartCardId = smartcardid;
        }

        public static async Task<CardModel> BuildCardModelAsync(Mfrc522 mfrc)
        {
            string SmartCardId = await GetSmartCardIdAsync(mfrc);
            return new CardModel(SmartCardId);
        }

        public static async Task<string> GetSmartCardIdAsync(Mfrc522 mfrc)
        {
            Uid uid = null;
            if (await mfrc.IsTagPresent())
            {
                uid = await mfrc.ReadUid();
                await mfrc.HaltTag();
            }
            if (uid != null)
                return uid.ToString();
            else
                return null;
        }
    }

    public class CardDetectedEventArgs
    {
        public CardModel Card { get; set; }
        public CardDetectedEventArgs(CardModel card)
        {
            this.Card = card;
        }
    }
}
