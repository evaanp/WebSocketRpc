﻿using EP94.WebSocketRpc.Internal.Shared;
using EP94.WebSocketRpc.Internal.WebSocketRpcServer.Models;
using EP94.WebSocketRpc.Public.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace EP94.WebSocketRpc.Public
{
    public abstract class WebSocketRpcServerV2
    {
        private HttpListener _listener;
        private Thread _acceptConnectionsThread;
        private List<WebSocketSession> _sessions = new List<WebSocketSession>();
        public WebSocketRpcServerV2(int port)
        {
            _listener = new HttpListener();
#if DEBUG
            _listener.Prefixes.Add($"http://{IPAddress.Loopback}:{port}/");
#else
            _listener.Prefixes.Add($"http://{IPAddress.Any}:{port}/");
#endif
            _acceptConnectionsThread = new Thread(() => ListenToConnections());
            _listener.Start();
            _acceptConnectionsThread.Start();
        }

        public void Broadcast(string eventName, object data)
        {
            foreach (var session in _sessions.Where(s => s.Open).ToList())
            {
                try
                {
                    session.Send(JsonConvert.SerializeObject(new BroadcastMessage() { EventName = eventName, Data = data }));
                }
                catch { }
            }
        }

        private async void ListenToConnections()
        {
            while (true)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        var socket = await context.AcceptWebSocketAsync(null);
                        var session = new WebSocketSession(socket.WebSocket, this);
                        _sessions.Add(session);
                        session.OnClose += (_, e) => RemoveOldConnections();
                    }
                }
                catch { }
            }
        }

        private void RemoveOldConnections()
        {
            _sessions.RemoveAll(s => !s.Open);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}