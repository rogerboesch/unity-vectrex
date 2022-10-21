using System;

// Wrapper class, later handling with proper interface

namespace RB
{
    public class Log
    {
        public static void Message(string text)
        {
            if (AppController.Instance == null) { return; }

            AppController.Instance.AddMessage(text);
        }
        public static void Error(string text)
        {
            if (AppController.Instance == null) { return; }

            AppController.Instance.AddMessage($"!!> {text}");
        }
    }
}
