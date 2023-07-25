using System;
using System.Net;

namespace AsyncProcessor
{
    public class ErrorEventException : Exception
    {
        public ErrorEventException()
            : base("Invalid construct")
        { }

        public ErrorEventException(string message)
            : base(message)
        { }

        public ErrorEventException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
