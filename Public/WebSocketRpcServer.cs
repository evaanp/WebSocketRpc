using System;
using System.Collections.Generic;
using System.Text;
using EP94.WebSocketRpc.Internal.Shared;
using EP94.WebSocketRpc.Internal.Shared.Interfaces;
using EP94.WebSocketRpc.Internal.Shared.Models;
using EP94.WebSocketRpc.Public.Shared;
using EP94.WebSocketRpc.Public.Shared.Models;
using WebSocketSharp.Server;

namespace EP94.WebsocketRpc.Public
{
    public abstract class WebSocketRpcServer : ICallable
    {
        private readonly WebSocketServer socket;
        public WebSocketRpcServer(int port)
        {
            socket = new WebSocketServer(port);
            socket.AddWebSocketService("/", () => new WebSocketServerSession(this));
            socket.Start();
        }

        public void Broadcast(string message)
        {
            socket.WebSocketServices.Broadcast(message.GetBytes());
        }
    }
}
