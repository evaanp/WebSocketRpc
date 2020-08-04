using EP94.WebSocketRpc.Internal.Shared;
using EP94.WebSocketRpc.Internal.Shared.Models;
using EP94.WebSocketRpc.Internal.Shared.Models.Responses;
using EP94.WebSocketRpc.Public;
using EP94.WebSocketRpc.Public.Exceptions;
using EP94.WebSocketRpc.Public.Shared.Models.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EP94.WebSocketRpc.Internal.WebSocketRpcServer.Models
{
    internal class WebSocketSession
    {
        public bool Open { get; set; } = true;
        public JsonRpcResponse JsonRpcResponse { get; private set; }
        public EventHandler OnClose;

        private Thread _thread;
        private WebSocket _socket;
        private WebSocketRpcServerV2 _server;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        public WebSocketSession(WebSocket socket, WebSocketRpcServerV2 server)
        {
            _socket = socket;
            _server = server;
            _thread = new Thread(() => ReceiveMessages());
            _thread.Start();
        }

        public async void Send(string message)
        {
            await _socket.SendAsync(message.GetBytes(), WebSocketMessageType.Text, true, _cts.Token);
        }

        private async void ReceiveMessages()
        {
            try
            {
                while (_socket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                    await _socket.ReceiveAsync(buffer, _cts.Token);
                    JsonRpcResponse response = ReceiveMessage(buffer.Array.GetString());
                    Send(response.ToJson());
                }
            }
            catch { }
            _cts.Cancel();
            //await _socket.CloseAsync(WebSocketCloseStatus.Empty, "", _cts.Token);
            _socket.Dispose();
            Open = false;
            OnClose?.Invoke(this, new EventArgs());
        }

        private JsonRpcResponse ReceiveMessage(string message)
        {
            JsonRpcResponse response;
            long messageId = 0;
            try
            {
                JsonRpcMessage jsonRpcMessage = JsonConvert.DeserializeObject<JsonRpcMessage>(message);
                messageId = jsonRpcMessage.Id;
                try
                {
                    response = new JsonRpcResponse(jsonRpcMessage.Id, TryCallMethod(jsonRpcMessage).Result);
                }
                catch (Exception e)
                {
                    throw e.InnerException;
                }
            }
            catch (Exception e)
            {
                response = e switch
                {
                    JsonSerializationException _ => new JsonRpcResponse(messageId, JsonRpcErrors.ParseError),
                    MethodNotFoundException _ => new JsonRpcResponse(messageId, JsonRpcErrors.MethodNotFound),
                    InvalidParametersException _ => new JsonRpcResponse(messageId, JsonRpcErrors.InvalidParams),
                    FormatException _ => new JsonRpcResponse(messageId, JsonRpcErrors.InvalidParams),
                    _ => new JsonRpcResponse(messageId, JsonRpcErrors.InternalError)
                };
            }
            return response;
        }

        private async Task<JToken> TryCallMethod(JsonRpcMessage message)
        {
            Type type = _server.GetType();
            MethodInfo methodInfo = type.GetMethod(message.Method);

            if (methodInfo == null)
                throw new MethodNotFoundException(message.Method);

            try
            {
                if (!TryConvertParameters(message, methodInfo, out object[] parameters))
                    throw new InvalidParametersException("");

                bool isAwaitable = methodInfo.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;

                object result = null;
                if (isAwaitable)
                {
                    if (methodInfo.ReturnType.IsGenericType)
                    {
                        result = (object)await (dynamic)methodInfo.Invoke(_server, parameters);
                    }
                    else
                    {
                        await (Task)methodInfo.Invoke(_server, parameters);
                    }
                }
                else
                {
                    if (methodInfo.ReturnType == typeof(void))
                    {
                        methodInfo.Invoke(_server, parameters);
                    }
                    else
                    {
                        result = methodInfo.Invoke(_server, parameters);
                    }
                }

                return JToken.FromObject(result);
            }
            catch (NullReferenceException)
            {
                throw new MethodNotFoundException(message.Method);
            }
        }

        private bool TryConvertParameters(JsonRpcMessage message, MethodInfo methodInfo, out object[] parameters)
        {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
           parameters = new object[parameterInfos.Length];

            if (parameterInfos.Length != message.Params.Length)
                return false;


            for (int i = 0; i < parameterInfos.Length; i++)
            {
                parameters[i] =  message.Params[i].ToObject(parameterInfos[i].ParameterType);
            }
            return true;
        }
    }
}
