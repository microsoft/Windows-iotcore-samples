//
// Copyright (c) Microsoft. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace EdgeModuleSamples.Common.Logging
{
    public static class Log
    {
        private const string fmt_highlight = "\u001b[94;40m";  // bright blue on black
        private const string fmt_output_default = "\u001b[0m"; // revert to default
        private const string fmt_output_error = "\u001b[91;40m";  // red on black
        private const string fmt_output_success = "\u001b[92;40m";  // green on black
        private const string fmt_output_up1 = "\u001b[1A"; // move up 1 line
        private const string fmt_output_up3 = "\u001b[3A"; // move up 3 line
        private const string fmt_output_clear_eol = "\u001b[K"; // clear current line from cursor to end of line
        private const string fmt_output_home = "\u001b[1G"; // move cursor to beginning of line

        private static string TimeStamp()
        {
            return DateTime.Now.ToLocalTime().ToString("yyyy/MM/dd:HH:mm:ss:FFFFFFF") + ": ";
        }
        public static bool Enabled { get; set; } = true;

        public static bool Verbose { get; set; } = false;

        public static void Write(string fmt, params object[] args)
        {
            if (Enabled)
            {
                Console.Write(fmt, args);
            }
        }
        public static void WriteLine(string fmt, params object[] args)
        {
            TimeStampedWriteLine(fmt, args);
        }

        public static void EndLine()
        {
            WriteInternal("");
        }
        public static void WriteLineVerbose(string fmt, params object[] args)
        {
            if (Verbose)
            {
                TimeStampedWriteLine(fmt, args);
            }
        }
        public static void WriteLineError(string fmt, params object[] args)
        {
            TimeStampedWriteLine("[ERROR] " + fmt_output_error + fmt + fmt_output_default, args);
        }
        public static void WriteLineException(Exception ex, bool writestack = true)
        {
            WriteLineInternal(DateTime.Now.ToLocalTime() + ": [ERROR] " + fmt_output_error + "{0} {1}" + fmt_output_default, ex.GetType().Name, ex.Message);

            if (writestack)
                WriteLineInternal(ex.StackTrace);
        }
        public static void WriteLineSuccess(string fmt, params object[] args)
        {
            TimeStampedWriteLine("[OK] " + fmt_output_success + fmt + fmt_output_default, args);
        }
        public static void WriteLineHome(string fmt, params object[] args)
        {
            WriteInternal(fmt_output_home + fmt_output_clear_eol);
            TimeStampedWriteLine(fmt, args);
        }
        public static void WriteLineUpHome(string fmt, params object[] args)
        {
            WriteInternal(fmt_output_up1 + fmt_output_home + fmt_output_clear_eol);
            TimeStampedWriteLine(fmt, args);
        }
        public static void WriteLineUp3Home(string fmt, params object[] args)
        {
            WriteInternal(fmt_output_up3 + fmt_output_home + fmt_output_clear_eol);
            TimeStampedWriteLine(fmt, args);
        }

        private static void TimeStampedWriteLine(string fmt, params object[] args)
        {
            WriteLineInternal(TimeStamp() + ": " + fmt, args);
        }
        private static void WriteLineInternal(string fmt, params object[] args)
        {
            if (Enabled)
            {
                if (null == args)
                {
                    Console.WriteLine(fmt);
                }
                else
                {
                    Console.WriteLine(fmt, args);
                }
            }
        }
        private static void WriteInternal(string fmt, params object[] args)
        {
            if (null == args)
            {
                Console.Write(fmt);
            }
            else
            {
                Console.Write(fmt, args);
            }
        }
    }
}
