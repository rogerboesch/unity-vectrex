using System;

// Wrapper class, later handling with proper interface

namespace RB
{
    public class Log
    {
        private static int s_lineNumber = 0;
        private static bool s_debug = false;

        public static void Debug(string text)
        {
            if (!s_debug) { return; }
            if (AppController.Instance == null) { return; }

            s_lineNumber++;

            AppController.Instance.AddMessage($"{s_lineNumber} {text}");
        }

        public static void Message(string text)
        {
            if (AppController.Instance == null) { return; }

            s_lineNumber++;

            AppController.Instance.AddMessage($"{s_lineNumber} {text}");
        }

        public static void Error(string text)
        {
            if (AppController.Instance == null) { return; }

            s_lineNumber++;

            AppController.Instance.AddMessage($"{s_lineNumber} !! {text}");
        }
    }
}
