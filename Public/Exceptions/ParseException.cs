using System;
using System.Collections.Generic;
using System.Text;

namespace EP94.WebSocketRpc.Public.Exceptions
{
    public class ParseException : Exception 
    {
        public ParseException(string message)
            : base(message) { }
    }
}
