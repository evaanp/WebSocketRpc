using System;
using System.Collections.Generic;
using System.Text;

namespace EP94.WebSocketRpc.Internal.Shared.Models.Responses
{
    internal struct JsonRpcError
    {
        public readonly int Code;
        public readonly string Message;
        public JsonRpcError(int code, string message)
        {
            Code = code;
            Message = message;
        }
    }
}
