using System;
using System.Collections.Generic;
using System.Text;

namespace EP94.WebSocketRpc.Public.Exceptions
{
    public class InvalidParametersException : Exception
    {
        public InvalidParametersException(string message)
            : base(message) { }
    }
}
