using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EP94.WebSocketRpc.Internal.Shared.Models
{
    internal class JsonRpcMessage
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpcVersion { get; set; } = "2.0";
        public string Method { get; set; }
        public object[] Params { get; set; }
        public long Id { get; set; }
        private static long _id;

        public JsonRpcMessage() { }
        public JsonRpcMessage(string method, params object[] parameters)
        {
            Method = method;
            Params = parameters;
            _id = _id < long.MaxValue ? ++_id : 1;
            Id = _id;
        }
    }
}
