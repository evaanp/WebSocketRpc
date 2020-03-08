﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using EP94.WebSocketRpc.Internal.WebSocketRpcServer.Models.Responses;

namespace EP94.WebSocketRpc.Internal.WebSocketRpcClient.Models
{
    internal class Subscription
    {
        public long Id;
        public ManualResetEvent resetEvent = new ManualResetEvent(false);
        public JsonRpcResponse response;

        public Subscription(long id)
        {
            Id = id;
        }
    }
}