using System;
using System.Collections.Generic;
using System.Text;

namespace EP94.WebSocketRpc.Internal.WebSocketRpcServer.Models.Responses
{
    internal struct JsonRpcError
    {
        public int Code;
        public string Message;
        public JsonRpcError(int code, string message)
        {
            Code = code;
            Message = message;
        }
    }
}
