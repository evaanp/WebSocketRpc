using EP94.WebSocketRpc.Internal.Shared;
using EP94.WebSocketRpc.Internal.Shared.Models;
using EP94.WebSocketRpc.Internal.Shared.Models.Responses;
using EP94.WebSocketRpc.Public.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EP94.WebSocketRpc.Public
{
    public class WebSocketRpcClientV2 : IDisposable
    {
        public event EventHandler<bool> OnConnectionChange;
        public event EventHandler<string> OnError;
        public string IpAddress { get; private set; }
        public int Port { get; private set; }
        private string Password { get; set; }
        public string UrlScheme { get; private set; }

        public bool Connected 
        {
            get
            {
                return _connected;
            }
            private set
            {
                if (_connected != value)
                {
                    _connected = value;
                    OnConnectionChange?.Invoke(this, value);
                }
            }
        }
        private bool _connected = false;
        private bool _error = false;

        private Dictionary<string, List<Action<JObject>>> _broadcastEventSubscriptions = new Dictionary<string, List<Action<JObject>>>(StringComparer.OrdinalIgnoreCase);
        private LinkedList<MethodCall> _sendBuffer = new LinkedList<MethodCall>();
        private HashSet<MethodCall> _methodCalls = new HashSet<MethodCall>();
        private ClientWebSocket _client;
        private object _clientLock = new object();
        private Thread _receiveValuesThread;
        private Thread _sendCallsThread;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        public WebSocketRpcClientV2(string ipAddress, int port, string password = "", bool secure = false)
        {
            IpAddress = ipAddress;
            Port = port;
            Password = password;
            UrlScheme = secure ? "wss" : "ws";
            _client = new ClientWebSocket();
            _receiveValuesThread = new Thread(() => RecieveValues());
            _sendCallsThread = new Thread(() => SendCalls());
        }
        public void Run()
        {
            Connect();
            _receiveValuesThread.Start();
            _sendCallsThread.Start();
        }

        public TReturnValue Call<TReturnValue>(string method, params object[] parameters)
        {
            MethodCall methodCall = new MethodCall(method, parameters);
            _sendBuffer.AddLast(methodCall);
            TReturnValue returnValue;
            try
            {
                returnValue = methodCall.AwaitResponse<TReturnValue>().Result;
            }
            catch
            {
                _methodCalls.Remove(methodCall);
                throw;
            }
            _methodCalls.Remove(methodCall);
            return returnValue;
        }

        public void Subscribe<T>(string eventName, Action<object> callback)
        {
            if (!_broadcastEventSubscriptions.ContainsKey(eventName))
                _broadcastEventSubscriptions.Add(eventName, new List<Action<JObject>>());
            _broadcastEventSubscriptions[eventName].Add((JObject value) => {
                try
                {
                    callback?.Invoke(value.ToObject<T>());
                }
                catch (Exception e)
                {
                    OnError?.Invoke(this, e.ToString());
                }
            });
        }

        private async void SendCalls()
        {
            while (!_cts.IsCancellationRequested)
            {
                if (_client.State != WebSocketState.Open)
                {
                    OnNotConnectedState();
                }
                if (_sendBuffer.Count > 0)
                {
                    try
                    {
                        MethodCall call;
                        while ((call = _sendBuffer.First.Value).CancellationTokenSource.IsCancellationRequested)
                            _sendBuffer.Remove(call);

                        ArraySegment<byte> sendBuffer = new ArraySegment<byte>(call.JsonRpcMessage.ToJson().Encrypt(Password).GetBytes());
                        await _client.SendAsync(sendBuffer, WebSocketMessageType.Text, true, _cts.Token);
                        _methodCalls.Add(call);
                        _sendBuffer.Remove(call);
                    }
                    catch
                    {
                        OnNotConnectedState();
                    }
                }
                Thread.Sleep(1);
            }
        }

        private async void RecieveValues()
        {
            while (!_cts.IsCancellationRequested)
            {
                if (_client.State == WebSocketState.Open)
                {
                    try
                    {
                        ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                        await _client.ReceiveAsync(buffer, _cts.Token);
                        OnReceive(buffer.Array.GetString().Decrypt(Password));
                    }
                    catch
                    {
                        OnNotConnectedState();
                    }
                }
                else
                {
                    OnNotConnectedState();
                }
            }
        }

        private void OnReceive(string message)
        {
            JObject jObject = JObject.Parse(message);
            string broadcastEventName = jObject.GetValue(nameof(BroadcastMessage.EventName), StringComparison.OrdinalIgnoreCase)?.Value<string>();
            if (broadcastEventName != null)
            {
                if (_broadcastEventSubscriptions.ContainsKey(broadcastEventName))
                    _broadcastEventSubscriptions[broadcastEventName].ForEach(a => a?.Invoke(jObject[nameof(BroadcastMessage.Data)].Value<JObject>()));
            }
            else
            {
                JsonRpcResponse jsonRpcResponse = jObject.ToObject<JsonRpcResponse>();
                MethodCall call = _methodCalls.ToList().FirstOrDefault(c => c.JsonRpcMessage.Id == jsonRpcResponse.Id);
                if (jsonRpcResponse.Error.HasValue)
                {
                    string methodCallString = call == null ? string.Empty : $" ({call})";
                    OnError?.Invoke(this, $"Error from server: {jsonRpcResponse.Error.Value.Message}{methodCallString}");
                    return;
                }
                if (call != null)
                    call.JsonRpcResponse = jsonRpcResponse;
            }
        }

        private void OnNotConnectedState()
        {
            lock (_clientLock)
            {
                if (!_cts.IsCancellationRequested)
                {
                    Connected = _client.State == WebSocketState.Open;
                    if (!Connected)
                    {
                        if (_client != null)
                            _client.Dispose();
                        _client = new ClientWebSocket();
                        Connect();
                    }
                }
            }
        }

        private void Connect()
        {
            try
            {
                _client.ConnectAsync(new Uri($"{UrlScheme}://{IpAddress}:{Port}"), _cts.Token).Wait();
                _error = false;
            }
            catch (Exception e) 
            {
                if (!_error)
                {
                    OnError?.Invoke(this, e.ToString());
                    _error = true;
                }
                Thread.Sleep(500);
            }
            Connected = _client.State == WebSocketState.Open;
        }

        public void Dispose()
        {
            if (_cts.IsCancellationRequested)
                throw new InvalidOperationException("Object already disposed");
            lock (_clientLock)
            {
                if (_client != null)
                    _client.Dispose();
                _client = null;
            }
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
