using EP94.WebSocketRpc.Internal.Shared.Models.Responses;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EP94.WebSocketRpc.Internal.Shared.Models
{
    internal class MethodCall
    {
        public JsonRpcMessage JsonRpcMessage { get; set; }
        public JsonRpcResponse JsonRpcResponse { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public MethodCall(string methodName, params object[] parameters)
        {
            JToken[] jTokens = new JValue[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                jTokens[i] = JToken.FromObject(parameters[i]);

            JsonRpcMessage = new JsonRpcMessage(methodName, jTokens);
            CancellationTokenSource = new CancellationTokenSource(5000);
        }

        public Task<T> AwaitResponse<T>()
        {
            return Task<T>.Factory.StartNew(() =>
            {
                while (JsonRpcResponse == null)
                {
                    if (CancellationTokenSource.IsCancellationRequested)
                        throw new Exception("JsonRpcTimeout");
                    Task.Delay(1).Wait();
                }
                return JsonRpcResponse.Result.ToObject<T>();
            });
        }

        public Task AwaitResponse()
        {
            return Task.Factory.StartNew(() =>
            {
                while (JsonRpcResponse == null)
                {
                    if (CancellationTokenSource.IsCancellationRequested)
                        throw new Exception("JsonRpcTimeout");
                    Task.Delay(1).Wait();
                }
            });
        }

        public override int GetHashCode()
        {
            return JsonRpcMessage.Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{JsonRpcMessage.Method}({string.Join(", ", JsonRpcMessage.Params.Values())})";
        }
    }
}
