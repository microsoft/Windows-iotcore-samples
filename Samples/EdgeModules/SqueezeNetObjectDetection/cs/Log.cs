using System;
using System.Collections.Generic;
using System.Text;

namespace Helpers
{
    static class Log
    {
        private const string fmt_highlight = "\u001b[94;40m";  // bright blue on black
        private const string fmt_output_default = "\u001b[0m"; // revert to default
        private const string fmt_output_error = "\u001b[91;40m";  // red on black
        private const string fmt_output_success = "\u001b[92;40m";  // green on black
        private const string fmt_output_up1 = "\u001b[1A"; // move up 1 line
        private const string fmt_output_up3 = "\u001b[3A"; // move up 3 line
        private const string fmt_output_clear_eol = "\u001b[K"; // clear current line from cursor to end of line
        private const string fmt_output_home = "\u001b[1G"; // move cursor to beginning of line

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
            WriteLineInternal(DateTime.Now.ToLocalTime() + ": " + fmt, args);
        }
        public static void WriteLineRaw(string message)
        {
            if (Enabled)
            {
                Console.WriteLine(DateTime.Now.ToLocalTime() + ": " + message);
            }
        }
        public static void WriteLineVerbose(string fmt, params object[] args)
        {
            if (Verbose)
            {
                WriteLineInternal(DateTime.Now.ToLocalTime() + ": " + fmt, args);
            }
        }
        public static void WriteLineError(string fmt, params object[] args)
        {
            WriteLineInternal(DateTime.Now.ToLocalTime() + ": [ERROR] " + fmt_output_error + fmt + fmt_output_default, args);
        }
        public static void WriteLineHome(string fmt, params object[] args)
        {
            WriteLineInternal(fmt_output_home + fmt_output_clear_eol + fmt, args);
        }
        public static void WriteLineUpHome(string fmt, params object[] args)
        {
            WriteLineInternal(fmt_output_up1 + fmt_output_home + fmt_output_clear_eol + fmt, args);
        }
        public static void WriteLineUp3Home(string fmt, params object[] args)
        {
            WriteLineInternal(fmt_output_up3 + fmt_output_home + fmt_output_clear_eol + fmt, args);
        }

        private static void WriteLineInternal(string fmt, params object[] args)
        {
            if (Enabled)
            {
                Console.WriteLine(fmt, args);
            }
        }

    }
}
