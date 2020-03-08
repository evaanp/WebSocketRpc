using System;
using System.Collections.Generic;
using System.Text;
using EP94.WebSocketRpc.Internal.Shared.Models.Responses;
using EP94.WebSocketRpc.Public.Exceptions;

namespace EP94.WebSocketRpc.Public.Shared.Models.Responses
{
    static class JsonRpcErrors
    {
        public readonly static JsonRpcError ParseError = new JsonRpcError(-32700, "Parse error");
        public readonly static JsonRpcError MethodNotFound = new JsonRpcError(-32601, "Method not found");
        public readonly static JsonRpcError InvalidRequest = new JsonRpcError(-32600, "Invalid Request");
        public readonly static JsonRpcError InternalError = new JsonRpcError(-32603, "Internal error");
        public readonly static JsonRpcError InvalidParams = new JsonRpcError(-32602, "Invalid params");
    }
}
