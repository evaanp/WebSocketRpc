using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EP94.WebSocketRpc.Internal.Shared.Models.Responses
{
    internal class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")]
        public readonly string JsonRpcVersion = "2.0";
        public long Id { get; set; }
        public JsonRpcError? Error { get; set; } = null;
        public object Result { get; set; } = null;

        public JsonRpcResponse() { }

        public JsonRpcResponse(long id, JsonRpcError error) 
        {
            Id = id;
            Error = error;
        }

        public JsonRpcResponse(long id, object result)
        {
            Id = id;
            Result = result;
        }
    }
}
