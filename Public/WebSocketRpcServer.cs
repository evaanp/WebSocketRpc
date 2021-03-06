﻿using EP94.WebSocketRpc.Internal.Shared;
using EP94.WebSocketRpc.Internal.WebSocketRpcServer.Models;
using EP94.WebSocketRpc.Public.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace EP94.WebSocketRpc.Public
{
    public abstract class WebSocketRpcServer
    {
        private HttpListener _listener;
        private Thread _acceptConnectionsThread;
        private List<WebSocketSession> _sessions = new List<WebSocketSession>();
        private Dictionary<int, string> _serverConfigurations;
        public WebSocketRpcServer(int port, string password = "")
        {
            _serverConfigurations = new Dictionary<int, string>();
            _serverConfigurations[port] = password;
            Init();
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverConfigurations">Dictionary<int port, string password></param>
        public WebSocketRpcServer(Dictionary<int, string> serverConfigurations)
        {
            _serverConfigurations = serverConfigurations;
            Init();
        }

        private void Init()
        {
            _listener = new HttpListener();
            foreach (int port in _serverConfigurations.Keys)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _listener.Prefixes.Add($"http://+:{port}/");
                }
                else
                {
                    _listener.Prefixes.Add($"http://*:{port}/");
                }
            }
            _acceptConnectionsThread = new Thread(() => ListenToConnections());
            _listener.Start();
            _acceptConnectionsThread.Start();
        }

        public void Broadcast(string eventName, object data)
        {
            foreach (var session in _sessions.Where(s => s.Open).ToList())
            {
                session.Send(JsonConvert.SerializeObject(new BroadcastMessage() { EventName = eventName, Data = data }));
            }
        }

        private async void ListenToConnections()
        {
            while (true)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    string password = _serverConfigurations[context.Request.Url.Port];
                    if (context.Request.IsWebSocketRequest)
                    {
                        var socket = await context.AcceptWebSocketAsync(null);
                        var session = new WebSocketSession(socket.WebSocket, this, password);
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
