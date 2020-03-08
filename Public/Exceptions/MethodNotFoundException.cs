using System;
using System.Collections.Generic;
using System.Text;

namespace EP94.WebSocketRpc.Public.Exceptions
{
    public class MethodNotFoundException : Exception 
    {
        public MethodNotFoundException(string message)
            : base(message) { }
    }
}
