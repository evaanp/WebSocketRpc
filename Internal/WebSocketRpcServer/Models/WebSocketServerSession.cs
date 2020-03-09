using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EP94.WebSocketRpc.Internal.Shared.Interfaces;
using EP94.WebSocketRpc.Internal.Shared.Models.Responses;
using EP94.WebSocketRpc.Public.Exceptions;
using EP94.WebSocketRpc.Public.Shared;
using EP94.WebSocketRpc.Public.Shared.Models.Responses;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace EP94.WebSocketRpc.Internal.Shared.Models
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
                try
                {
                    response = new JsonRpcResponse(message.Id, TryCallMethod(message).Result).ToJson();
                }
                catch (AggregateException ex)
                {
                    throw ex.InnerException;
                }
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

        private async Task<object> TryCallMethod(JsonRpcMessage message)
        {
            Type type = callable.GetType();
            MethodInfo methodInfo = type.GetMethod(message.Method);

            if (methodInfo == null)
                throw new MethodNotFoundException(message.Method);

            try
            {
                if (!TryConvertParameters(message, methodInfo))
                    throw new InvalidParametersException("");

                var isAwaitable = methodInfo.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;

                object invokeResult = null;
                if (isAwaitable)
                {
                    if (methodInfo.ReturnType.IsGenericType)
                    {
                        invokeResult = (object)await (dynamic)methodInfo.Invoke(callable, message.Params);
                    }
                    else
                    {
                         await (Task)methodInfo.Invoke(callable, message.Params);
                    }
                }
                else
                {
                    if (methodInfo.ReturnType == typeof(void))
                    {
                        methodInfo.Invoke(callable, message.Params);
                    }
                    else
                    {
                        invokeResult = methodInfo.Invoke(callable, message.Params);
                    }
                }

                return invokeResult;
            }
            catch (NullReferenceException)
            {
                throw new MethodNotFoundException(message.Method);
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
