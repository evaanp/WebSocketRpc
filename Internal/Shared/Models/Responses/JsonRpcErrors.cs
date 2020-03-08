using System;
using System.Collections.Generic;
using System.Text;

namespace EP94.WebSocketRpc.Internal.WebSocketRpcServer.Models.Responses
{
    static class JsonRpcErrors
    {
        public static JsonRpcError ParseError = new JsonRpcError(-32700, "Parse error");
        public static JsonRpcError MethodNotFound = new JsonRpcError(-32601, "Method not found");
        public static JsonRpcError InvalidRequest = new JsonRpcError(-32600, "Invalid Request");
        public static JsonRpcError InternalError = new JsonRpcError(-32603, "Internal error");
        public static JsonRpcError InvalidParams = new JsonRpcError(-32602, "Invalid params");
    }
}
