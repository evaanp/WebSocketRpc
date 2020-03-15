using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EP94.WebSocketRpc.Internal.Shared;
using EP94.WebSocketRpc.Internal.Shared.Models;
using EP94.WebSocketRpc.Internal.Shared.Models.Responses;
using EP94.WebSocketRpc.Internal.WebSocketRpcClient.Models;
using EP94.WebSocketRpc.Public.Models;
using EP94.WebSocketRpc.Public.Shared;
using EP94.WebSocketRpc.Public.Shared.Models;
using EP94.WebSocketRpc.Public.Shared.Models.Responses;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using WebSocketSharp;

namespace EP94.WebSocketRpc.Public
{
    
    public class WebSocketRpcClient
    {
        public delegate void BroadcastMessageDelegate(BroadcastMessage broadcastMessage);
        public BroadcastMessageDelegate OnBroadcastMessage;
        private WebSocket webSocket;
        private List<Subscription> subscriptions = new List<Subscription>();
        private ManualResetEvent resetEvent;
        private delegate void Error(string error);
        private Error OnError;
        private bool connected = false;
        private IPAddress IPAddress;
        private object _lock = new object();
        public WebSocketRpcClient(IPAddress iPAddress, int port, bool secure, ILogger logger)
        {
            Log.Logger = logger;

            this.IPAddress = iPAddress;

            string protocol = secure ? "wss" : "ws";

            webSocket = new WebSocket($"{protocol}://{iPAddress.ToString()}:{port}");

            webSocket.Log.Output = (data, error) =>
            {
                OnError?.Invoke(error);
            };
            
            webSocket.OnMessage += (sender, e) => ReceiveMessages(e.Data);
            webSocket.OnClose += (sender, e) =>
            {
                if (connected)
                    Log.Error($"Lost connection to {iPAddress.ToString()}");
                connected = false;
                Connect();
            };
            webSocket.OnOpen += (sender, e) =>
            {
                if (!connected)
                    Log.Information($"Connected to {iPAddress.ToString()}");
                connected = true;
                if (resetEvent != null)
                {
                    resetEvent.Set();
                }
            };
            webSocket.OnError += (sender, e) => Log.Error(e.Message);
        }

        public bool Connect()
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(2000);
                resetEvent = new ManualResetEvent(false);

                var task = Task.Run(() =>
                {
                    webSocket.Connect();
                }, cancellationTokenSource.Token);

                while (resetEvent != null && !resetEvent.WaitOne(1) && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Thread.Sleep(1);
                }

                if (cancellationTokenSource.IsCancellationRequested)
                    Log.Error($"Connection timed out {IPAddress.ToString()}");

                cancellationTokenSource.Dispose();

                if (resetEvent != null)
                    resetEvent.Dispose();
                resetEvent = null;
            }
            catch (Exception e)
            {
                Log.Error(e, "");
                Task.Run(() => Connect());
            }
            return connected;
        }

        public async Task<T> Call<T>(string method, params object[] parameters)
        {
            if (!connected)
                throw new Exception($"Not connected {IPAddress.ToString()}");
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(5000);
            JsonRpcMessage message = new JsonRpcMessage(method, parameters);
            Task<JsonRpcResponse> subscription = SubscribeToResponse(message.Id);
            webSocket.Send(message.ToJson());
            subscription.Wait(cancellationTokenSource.Token);
            var result = await subscription;
            if (result.Error != null)
            {
                throw new Exception(result.Error.Value.Message);
            }
            return (T)Convert.ChangeType(result.Result, typeof(T));
        }

        public async Task<bool> Call(string method, params object[] parameters)
        {
            if (!connected)
                throw new Exception("Not connected");
            JsonRpcMessage message = new JsonRpcMessage(method, parameters);
            Task<JsonRpcResponse> subscription = SubscribeToResponse(message.Id);
            webSocket.Send(message.ToJson());
            var result = await subscription;
            if (result.Error != null)
            {
                throw new Exception(result.Error.Value.Message);
            }
            return (string)result.Result == "OK";
        }

        private void ReceiveMessages(string message)
        {
            try
            {
                if (message.Contains(nameof(BroadcastMessage.EventName)))
                {
                    BroadcastMessage broadcastMessage = JsonConvert.DeserializeObject<BroadcastMessage>(message);
                    OnBroadcastMessage?.Invoke(broadcastMessage);
                }
                else
                {
                    JsonRpcResponse response = JsonConvert.DeserializeObject<JsonRpcResponse>(message);
                    foreach (Subscription subscription in subscriptions.ToArray())
                    {
                        if (subscription.Id == response.Id)
                        {
                            subscriptions.Remove(subscription);
                            subscription.response = response;
                            subscription.resetEvent.Set();
                        }
                        if (subscription.CreationDateTime.AddSeconds(5) <= DateTime.UtcNow)
                        {
                            subscriptions.Remove(subscription);
                        }
                    }
                }                 
            }
            catch (Exception e) 
            {
                Log.Error(e, "");
            }
        }

        private async Task<JsonRpcResponse> SubscribeToResponse(long id)
        {
            return await Task.Run(() =>
            {
                Subscription subscription = new Subscription(id);
                subscriptions.Add(subscription);
                subscription.resetEvent.WaitOne();
                return subscription.response;
            });          
        }
    }
}
