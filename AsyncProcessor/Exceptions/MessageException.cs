using System;
using System.Net;

namespace AsyncProcessor
{
    public class MessageException : Exception
    {
        public MessageException()
            : base("Invalid construct")
        { }

        public MessageException(string message)
            : base(message)
        { }

        public MessageException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
