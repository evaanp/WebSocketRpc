using System;
using System.Collections.Generic;
using System.Text;
using EP94.WebSocketRpc.Internal.Shared.Models;
using EP94.WebSocketRpc.Internal.Shared.Models.Responses;
using EP94.WebSocketRpc.Public.Shared.Models;
using EP94.WebSocketRpc.Public.Shared.Models.Responses;
using Newtonsoft.Json;

namespace EP94.WebSocketRpc.Internal.Shared
{
    internal static class Extensions
    {
        public static byte[] GetBytes(this string message)
        {
            return Encoding.Default.GetBytes(message);
        }

        public static string GetString(this byte[] bytes)
        {
            return Encoding.Default.GetString(bytes);
        }

        public static string ToJson(this JsonRpcResponse jsonRpcResponse)
        {
            return JsonConvert.SerializeObject(jsonRpcResponse,
                            Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
        }

        public static string ToJson(this JsonRpcMessage jsonRpcMessage)
        {
            return JsonConvert.SerializeObject(jsonRpcMessage,
                            Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
        }
    }
}
