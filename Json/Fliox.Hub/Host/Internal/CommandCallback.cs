// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Internal
{
    internal readonly struct InvokeResult
    {
        internal readonly   JsonValue   value;
        internal readonly   string      error;

        public override     string      ToString() => error ?? value.AsString();

        internal InvokeResult(byte[] value) {
            this.value  = new JsonValue(value);
            this.error  = null;
        }
        
        internal InvokeResult(string error) {
            this.value  = default;
            this.error  = error;
        }
    }
    
    internal abstract class CommandCallback
    {
        internal string     error;
        // return type could be a ValueTask but Unity doesnt support this. 2021-10-25
        internal abstract Task<InvokeResult> InvokeCallback(string messageName, JsonValue messageValue, MessageContext messageContext);
    }
    
    internal sealed class CommandCallback<TValue, TResult> : CommandCallback
    {
        private  readonly   string                              name;
        private  readonly   CmdHandler<TValue, TResult>         handler;

        public   override   string                              ToString() => name;

        internal CommandCallback (string name, CmdHandler<TValue, TResult> handler) {
            this.name       = name;
            this.handler    = handler;
        }
        
        internal override Task<InvokeResult> InvokeCallback(string messageName, JsonValue messageValue, MessageContext messageContext) {
            var     cmd     = new Command<TValue>(messageName, messageValue, messageContext, this);
            TResult result  = handler(cmd);
            if (error != null) {
                return Task.FromResult(new InvokeResult(error));
            }
            using (var pooled = messageContext.pool.ObjectMapper.Get()) {
                var jsonResult  = pooled.instance.WriteAsArray(result);
                return Task.FromResult(new InvokeResult(jsonResult));
            }
        }
    }
    
    internal sealed class CommandAsyncCallback<TParam, TResult> : CommandCallback
    {
        private  readonly   string                                          name;
        private  readonly   CmdHandler<TParam, Task<TResult>>   handler;

        public   override   string                              ToString() => name;

        internal CommandAsyncCallback (string name, CmdHandler<TParam, Task<TResult>> handler) {
            this.name       = name;
            this.handler    = handler;
        }
        
        internal override async Task<InvokeResult> InvokeCallback(string messageName, JsonValue messageValue, MessageContext messageContext) {
            var cmd     = new Command<TParam>(messageName, messageValue, messageContext, this);
            var result  = await handler(cmd).ConfigureAwait(false);
            if (error != null) {
                return new InvokeResult(error);
            }
            using (var pooled = messageContext.pool.ObjectMapper.Get()) {
                var writer = pooled.instance;
                writer.WriteNullMembers = cmd.WriteNull;
                var jsonResult          = writer.WriteAsArray(result);
                return new InvokeResult(jsonResult);
            }
        }
    }
}