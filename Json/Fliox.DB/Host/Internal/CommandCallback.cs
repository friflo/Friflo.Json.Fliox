// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host.Internal
{
    public delegate TResult CommandHandler<TValue, out TResult>(Command<TValue> command);
    
    internal abstract class CommandCallback
    {
        // return type could be a ValueTask but Unity doesnt support this. 2021-10-25
        internal abstract Task<JsonValue> InvokeCallback(string messageName, JsonValue messageValue, MessageContext messageContext);
    }
    
    internal sealed class CommandCallback<TValue, TResult> : CommandCallback
    {
        private  readonly   string                              name;
        private  readonly   CommandHandler<TValue, TResult>     handler;

        public   override   string                              ToString() => name;

        internal CommandCallback (string name, CommandHandler<TValue, TResult> handler) {
            this.name       = name;
            this.handler    = handler;
        }
        
        internal override Task<JsonValue> InvokeCallback(string messageName, JsonValue messageValue, MessageContext messageContext) {
            var     cmd     = new Command<TValue>(messageName, messageValue, messageContext);
            TResult result  = handler(cmd);
            using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                var jsonResult  = pooledMapper.instance.WriteAsArray(result);
                return Task.FromResult(new JsonValue(jsonResult));
            }
        }
    }
    
    internal sealed class CommandAsyncCallback<TValue, TResult> : CommandCallback
    {
        private  readonly   string                                  name;
        private  readonly   CommandHandler<TValue, Task<TResult>>   handler;

        public   override   string                                  ToString() => name;

        internal CommandAsyncCallback (string name, CommandHandler<TValue, Task<TResult>> handler) {
            this.name       = name;
            this.handler    = handler;
        }
        
        internal override async Task<JsonValue> InvokeCallback(string messageName, JsonValue messageValue, MessageContext messageContext) {
            var     cmd     = new Command<TValue>(messageName, messageValue, messageContext);
            TResult result  = await handler(cmd).ConfigureAwait(false);
            using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                var jsonResult  = pooledMapper.instance.WriteAsArray(result);
                return new JsonValue(jsonResult);
            }
        }
    }
}