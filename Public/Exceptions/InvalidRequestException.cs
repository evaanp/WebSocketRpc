using System;
using System.Collections.Generic;
using System.Text;

namespace EP94.WebSocketRpc.Public.Exceptions
{
    public class InvalidRequestException : Exception 
    {
        public InvalidRequestException(string message)
            : base(message) { }
    }
}
