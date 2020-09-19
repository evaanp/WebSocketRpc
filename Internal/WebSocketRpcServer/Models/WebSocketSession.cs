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
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EP94.WebSocketRpc.Internal.WebSocketRpcServer.Models
{
    internal class WebSocketSession : IDisposable
    {
        public bool Open { get; set; } = true;
        public JsonRpcResponse JsonRpcResponse { get; private set; }
        public EventHandler OnClose;

        private Thread _thread;
        private WebSocket _socket;
        private Public.WebSocketRpcServer _server;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private object _disposeLock = new object();
        private bool _disposed = false;
        private string _password;
        public WebSocketSession(WebSocket socket, Public.WebSocketRpcServer server, string password)
        {
            _socket = socket;
            _server = server;
            _password = password;
            _thread = new Thread(() => ReceiveMessages());
            _thread.Start();
        }

        public async void Send(string message)
        {
            message = !string.IsNullOrEmpty(_password) ? message.Encrypt(_password) : message;
            try
            {
                await _socket.SendAsync(message.GetBytes(), WebSocketMessageType.Text, true, _cts.Token);
            }
            catch
            {
                Dispose();
            }
        }

        private async void ReceiveMessages()
        {
            try
            {
                while (_socket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                    await _socket.ReceiveAsync(buffer, _cts.Token);
                    string msg = buffer.Array.GetString();
                    JsonRpcResponse response = ReceiveMessage(!string.IsNullOrEmpty(_password) ? msg.Decrypt(_password) : msg);
                    Send(response.ToJson());
                }
            }
            catch { }
            Dispose();
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
                Console.WriteLine(e);
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

            IEnumerable<MethodInfo> methodInfos = type.GetMethods().Where(m => m.Name == message.Method);

            int count = methodInfos.Count();

            if (count == 0)
                throw new MethodNotFoundException(message.Method);

            MethodInfo methodInfo = count > 1 ? methodInfos.Where(m => m.GetParameters().Length == message.Params.Length).FirstOrDefault() : methodInfos.FirstOrDefault();

            if (methodInfo == null)
                throw new InvalidParametersException("");

            try
            {
                object[] parameters;
                try
                {
                    TryConvertParameters(message, methodInfo, out parameters);
                }
                catch (Exception)
                {
                    throw new InvalidParametersException("");
                }

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

        private void TryConvertParameters(JsonRpcMessage message, MethodInfo methodInfo, out object[] parameters)
        {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            parameters = new object[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                parameters[i] =  message.Params[i].ToObject(parameterInfos[i].ParameterType);
            }
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (!_disposed)
                {
                    _cts.Cancel();
                    _disposed = true;
                    _socket.Dispose();
                    Open = false;
                }
            }
        }
    }
}
