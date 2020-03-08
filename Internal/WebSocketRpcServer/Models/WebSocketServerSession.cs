using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using EP94.WebSocketRpc.Internal.Shared;
using EP94.WebSocketRpc.Internal.WebSocketRpcServer.Exceptions;
using EP94.WebSocketRpc.Internal.WebSocketRpcServer.Interfaces;
using EP94.WebSocketRpc.Internal.WebSocketRpcServer.Models.Responses;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace EP94.WebSocketRpc.Internal.WebSocketRpcServer.Models
{
    internal class WebSocketServerSession : WebSocketBehavior
    {
        private ICallable callable;
        public WebSocketServerSession(ICallable callable) 
        {
            this.callable = callable;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            string response = "";
            JsonRpcMessage message = new JsonRpcMessage();
            try
            {
                message = JsonConvert.DeserializeObject<JsonRpcMessage>(e.Data);
                response = new JsonRpcResponse(message.Id, TryCallMethod(message)).ToJson();          
            }
            catch (JsonSerializationException)
            {
                response = new JsonRpcResponse(message.Id, JsonRpcErrors.ParseError).ToJson();
            }
            catch (MethodNotFoundException)
            {
                response = new JsonRpcResponse(message.Id, JsonRpcErrors.MethodNotFound).ToJson();
            }
            catch (InvalidParametersException)
            {
                response = new JsonRpcResponse(message.Id, JsonRpcErrors.InvalidParams).ToJson();
            }
            catch (FormatException)
            {
                response = new JsonRpcResponse(message.Id, JsonRpcErrors.InvalidParams).ToJson();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                response = new JsonRpcResponse(message.Id, JsonRpcErrors.InternalError).ToJson();
            }
            finally
            {
                Send(response);
            }
        }

        private object TryCallMethod(JsonRpcMessage message)
        {
            Type methodType = callable.GetType();
            MethodInfo m = methodType.GetMethod(message.Method);
            try
            {
                if (!TryConvertParameters(message, m))
                    throw new InvalidParametersException();
                return m?.Invoke(callable, message.Params) ?? "OK";
            }
            catch (NullReferenceException)
            {
                throw new MethodNotFoundException();
            }
        }

        private bool TryConvertParameters(JsonRpcMessage message, MethodInfo methodInfo)
        {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();

            if (parameterInfos.Length != message.Params.Length)
                return false;

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                message.Params[i] = Convert.ChangeType(message.Params[i], parameterInfos[i].ParameterType);
            }
            return true;
        }
    }
}
