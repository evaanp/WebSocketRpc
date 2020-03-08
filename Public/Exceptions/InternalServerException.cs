using System;
using System.Collections.Generic;
using System.Text;

namespace EP94.WebSocketRpc.Public.Exceptions
{
    public class InternalServerException : Exception
    {
        public InternalServerException(string message)
            : base(message) { }
    }
}
