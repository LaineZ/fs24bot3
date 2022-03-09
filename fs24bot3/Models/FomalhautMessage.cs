using System;

namespace fs24bot3.Models
{

    public enum Kind
    {
        Message,
        ServerMessage
    }
    public class FomalhautMessage
    {
        public string Nick { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public Kind Kind { get; set; }
    }
}
