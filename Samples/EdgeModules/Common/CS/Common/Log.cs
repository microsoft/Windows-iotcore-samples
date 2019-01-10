//
// Copyright (c) Microsoft. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace EdgeModuleSamples.Common
{
    public class Log
    {
        public const string fmt_highlight = "\u001b[94;40m";  // bright blue on black
        public const string fmt_output_default = "\u001b[0m"; // revert to default
        public const string fmt_output_error = "\u001b[91;40m";  // red on black
        public const string fmt_output_success = "\u001b[92;40m";  // green on black
        // 
        public const string fmt_output_up1 = "\u001b[1A"; // move up 1 line
        public const string fmt_output_up3 = "\u001b[3A"; // move up 3 line
        public const string fmt_output_clear_eol = "\u001b[K"; // clear current line from cursor to end of line
        public const string fmt_output_home = "\u001b[1G"; // move cursor to beginning of line

        public static bool Enabled { get; set; }
        public static void Write(string fmt, params object[] args)
        {
            if (Enabled)
            {
                Console.Write(fmt, args);
            }
        }
        public static void WriteLine(string fmt, params object[] args)
        {
            if (Enabled)
            {
                Console.Write(fmt + "\r\n", args);
            }
        }
        public static void WriteLineError(string fmt, params object[] args)
        {
            WriteLine(fmt_output_error + fmt + fmt_output_default, args);
        }
        public static void WriteLineHome(string fmt, params object[] args)
        {
            WriteLine(fmt_output_home + fmt_output_clear_eol + fmt, args);
        }
        public static void WriteLineUpHome(string fmt, params object[] args)
        {
            WriteLine(fmt_output_up1 + fmt_output_home + fmt_output_clear_eol + fmt, args);
        }
        public static void WriteLineUp3Home(string fmt, params object[] args)
        {
            WriteLine(fmt_output_up3 + fmt_output_home + fmt_output_clear_eol + fmt, args);
        }
        static Log()
        {
            Enabled = false;
        }
    }
}
