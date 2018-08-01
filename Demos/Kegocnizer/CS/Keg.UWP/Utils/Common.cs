using Keg.DAL.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Foundation;
using System.Reflection;

namespace Keg.UWP.Utils
{
    public static class Common
    {
        /// <summary>
        /// UX Related Contants
        /// </summary>
        public const Int32 COUNTERWAIT = 20;  //15 seconds
        public const Int32 COUNTERSHORTWAIT = 20; //5 seconds
        public const float MINIMUMLIMITTOEMPTY = 5.0f;
        public static double AppWindowWidth = 720; //Init
        public static double MaxTempInsideKeg = 45.0f;

        public static readonly char[] commaDelimiter = new char[] { ',' };
        public static readonly char[] semiColonDelimiter = new char[] { ';' };
        public static readonly char[] colonDelimiter = new char[] { ':' };
        public static readonly char[] timeTDelimiter = new char[] { 'T' };
       
        public static KegConfig KegSettings;

        public static async Task GetKegSettings()
        {
            if(Keg.DAL.Constants.KEGSETTINGSGUID.Trim().Length > 0 )
            {
                Common.KegSettings = await GlobalSettings.GetKegSetting(Keg.DAL.Constants.KEGSETTINGSGUID);

                if (Common.KegSettings == null)
                {
                    throw new Exception("Incorrect KegSettingsGuid, please correct Keg.DAL.Constants.KEGSETTINGSGUID.");
                }
            }
        }

        public static string GetResourceText(string keyText)
        {
            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            string strLoc = loader.GetString(keyText);

            //No Localized String, Check for en-US
            if (strLoc.Trim().Length == 0)
            {
                var newloader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView("en-US");
                strLoc = loader.GetString(keyText);
            }

            return strLoc;
        }

        public static Tuple<Size, DisplayOrientations> GetCurrentDisplaySize()
        {
            var displayInformation = DisplayInformation.GetForCurrentView();
            TypeInfo t = typeof(DisplayInformation).GetTypeInfo();
            var props = t.DeclaredProperties.Where(x => x.Name.StartsWith("Screen") && x.Name.EndsWith("InRawPixels")).ToArray();
            var w = props.Where(x => x.Name.Contains("Width")).First().GetValue(displayInformation);
            var h = props.Where(x => x.Name.Contains("Height")).First().GetValue(displayInformation);
            var size = new Size(System.Convert.ToDouble(w), System.Convert.ToDouble(h));
            switch (displayInformation.CurrentOrientation)
            {
                case DisplayOrientations.Landscape:
                case DisplayOrientations.LandscapeFlipped:
                    size = new Size(Math.Max(size.Width, size.Height), Math.Min(size.Width, size.Height));
                    break;
                case DisplayOrientations.Portrait:
                case DisplayOrientations.PortraitFlipped:
                    size = new Size(Math.Min(size.Width, size.Height), Math.Max(size.Width, size.Height));
                    break;
            }
            return new Tuple<Size, DisplayOrientations>(size, displayInformation.CurrentOrientation);
        }

    }


    internal static class NativeTimeMethods
    {
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern void GetLocalTime(out SYSTEMTIME lpLocalTime);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEMTIME
    {
        [MarshalAs(UnmanagedType.U2)]
        internal short Year;


        [MarshalAs(UnmanagedType.U2)]
        internal short Month;


        [MarshalAs(UnmanagedType.U2)]
        internal short DayOfWeek;


        [MarshalAs(UnmanagedType.U2)]
        internal short Day;


        [MarshalAs(UnmanagedType.U2)]
        internal short Hour;


        [MarshalAs(UnmanagedType.U2)]
        internal short Minute;


        [MarshalAs(UnmanagedType.U2)]
        internal short Second;


        [MarshalAs(UnmanagedType.U2)]
        internal short Milliseconds;

        internal DateTime ToDateTime()
        {
            return new DateTime(Year, Month, Day, Hour, Minute, Second);
        }

    }
}



