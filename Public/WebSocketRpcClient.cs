using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EP94.WebSocketRpc.Internal.Shared;
using EP94.WebSocketRpc.Internal.Shared.Models;
using EP94.WebSocketRpc.Internal.Shared.Models.Responses;
using EP94.WebSocketRpc.Internal.WebSocketRpcClient.Models;
using EP94.WebSocketRpc.Public.Shared;
using EP94.WebSocketRpc.Public.Shared.Models;
using EP94.WebSocketRpc.Public.Shared.Models.Responses;
using Newtonsoft.Json;
using WebSocketSharp;

namespace EP94.WebSocketRpc.Public
{
    public class WebSocketRpcClient
    {
        private WebSocket webSocket;
        private List<Subscription> subscriptions = new List<Subscription>();
        public WebSocketRpcClient()
        {
            webSocket = new WebSocket("ws://localhost:8080");
            webSocket.Connect();
            webSocket.Log.Output = (data, error) =>
            {
                
            };
            
            webSocket.OnMessage += (sender, e) => ReceiveMessages(e.Data);
            webSocket.OnClose += (sender, e) => webSocket.Connect();
        }

        public async Task<T> Call<T>(string method, params object[] parameters)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(2000);
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
            catch (Exception) { }
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
