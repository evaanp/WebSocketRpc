using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EP94.WebSocketRpc.Internal.Shared.Models.Responses
{
    internal class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")]
        public readonly string JsonRpcVersion = "2.0";

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("error")]
        public JsonRpcError? Error { get; set; } = null;

        [JsonProperty("result")]
        public JToken Result { get; set; } = null;

        public JsonRpcResponse() { }

        public JsonRpcResponse(long id, JsonRpcError error) 
        {
            Id = id;
            Error = error;
        }

        public JsonRpcResponse(long id, JToken result)
        {
            Id = id;
            Result = result;
        }
    }
}
