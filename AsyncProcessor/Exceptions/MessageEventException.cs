using System;
using System.Net;

namespace AsyncProcessor
{
    public class MessageEventException : Exception
    {
        public MessageEventException()
            : base("Invalid construct")
        { }

        public MessageEventException(string message)
            : base(message)
        { }

        public MessageEventException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
