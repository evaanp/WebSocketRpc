using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EP94.WebSocketRpc.Internal.Shared.Models.Responses
{
    internal struct JsonRpcError
    {
        [JsonProperty("code")]
        public readonly int Code;
        [JsonProperty("message")]
        public readonly string Message;
        public JsonRpcError(int code, string message)
        {
            Code = code;
            Message = message;
        }
    }
}
