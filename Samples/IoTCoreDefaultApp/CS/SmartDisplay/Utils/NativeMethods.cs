// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SmartDisplay.Utils
{
    public static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern Boolean EnumUILanguagesW(
            EnumUILanguagesProc lpUILanguageEnumProc,
            UInt32 dwFlags,
            IntPtr lParam
        );

        public delegate Boolean EnumUILanguagesProc(
            IntPtr lpUILanguageString,
            IntPtr lParam
        );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int ResolveLocaleName(string lpNameToResolve, StringBuilder lpLocaleName, int cchLocaleName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int LCIDToLocaleName(uint Locale, StringBuilder lpName, int cchName, int dwFlags);
    }
}
