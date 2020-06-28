using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Core
{
    public class IrcMessage
    {
        public string Message { get; set; }
        public DateTime Date { get; set; }
    }

    public static class HourstatHelper
    {
        public static List<IrcMessage> IRCMessages = new List<IrcMessage>();

        public static void InsertMessage(string message, DateTime date)
        {
            IRCMessages.Add(new IrcMessage() { Message = message, Date = date });
        }
    }
}
