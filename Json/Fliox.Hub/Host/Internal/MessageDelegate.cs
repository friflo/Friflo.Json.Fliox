// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.Internal
{
    internal readonly struct InvokeResult
    {
        internal readonly   JsonValue   value;
        internal readonly   ResultError error;
        internal            bool        Success     => error.message == null;

        public override     string      ToString()  => error.message ?? value.AsString();

        internal InvokeResult(in JsonValue value) {
            this.value  = value;
            error       = default;
        }
        
        internal InvokeResult(in ResultError error) {
            this.value  = default;
            this.error  = error;
        }
    }
    
    internal enum MsgType {
        Command = 1,
        Message = 2
    }
    
    // ----------------------------------- MessageDelegate -----------------------------------
    internal abstract class MessageDelegate
    {
        // Note! Must not contain any mutable state
        internal  readonly  string              name;
        internal  abstract  MsgType             MsgType { get; }  
        public    override  string              ToString()  => name;
        internal  abstract  bool                IsSynchronous     { get; }
        
        // return type could be a ValueTask but Unity doesn't support this. 2021-10-25
        internal  virtual   Task<InvokeResult> InvokeDelegateAsync(SyncRequestTask task, ShortString messageName, JsonValue messageValue, SyncContext syncContext)
            => throw new InvalidOperationException("expect asynchronous implementation");
        
        internal  virtual        InvokeResult  InvokeDelegate     (SyncRequestTask task, in ShortString messageName, in JsonValue messageValue, SyncContext syncContext)
            => throw new InvalidOperationException("expect synchronous implementation");
        
        protected MessageDelegate (string name) {
            this.name   = name;
        }
    }
    
    // ----------------------------------- MessageDelegate<> -----------------------------------
    internal sealed class MessageDelegate<TValue> : MessageDelegate
    {
        private  readonly   HostMessageHandler<TValue>  handler;
        internal override   MsgType                     MsgType         => MsgType.Message;
        internal override   bool                        IsSynchronous   => true;

        internal MessageDelegate (string name, HostMessageHandler<TValue> handler) : base(name){
            this.handler    = handler;
        }
        
        internal override InvokeResult InvokeDelegate(SyncRequestTask task, in ShortString messageName, in JsonValue messageValue, SyncContext syncContext) {
            var cmd     = new MessageContext(task, messageName,  syncContext);
            var param   = new Param<TValue> (messageValue, syncContext); 
            handler(param, cmd);
            
            return new InvokeResult(new JsonValue());
        }
    }
    
    // ----------------------------------- MessageDelegateAsync<> -----------------------------------
    internal sealed class MessageDelegateAsync<TParam> : MessageDelegate
    {
        private  readonly   HostMessageHandlerAsync<TParam>     handler;
        internal override   MsgType                             MsgType         => MsgType.Message;
        internal override   bool                                IsSynchronous   => false;

        internal MessageDelegateAsync (string name, HostMessageHandlerAsync<TParam> handler) : base(name) {
            this.handler    = handler;
        }
        
        internal override async Task<InvokeResult> InvokeDelegateAsync(SyncRequestTask task, ShortString messageName, JsonValue messageValue, SyncContext syncContext) {
            var cmd     = new MessageContext(task, messageName,  syncContext);
            var param   = new Param<TParam> (messageValue, syncContext); 
            await handler(param, cmd).ConfigureAwait(false);
            
            return new InvokeResult(new JsonValue());
        }
    }
    
    // ----------------------------------- CommandDelegate<,> -----------------------------------
    internal sealed class CommandDelegate<TValue, TResult> : MessageDelegate
    {
        private  readonly   HostCommandHandler<TValue, TResult> handler;
        internal override   MsgType                             MsgType         => MsgType.Command;
        internal override   bool                                IsSynchronous   => true;

        internal CommandDelegate (string name, HostCommandHandler<TValue, TResult> handler) : base(name){
            this.handler    = handler;
        }
        
        internal override InvokeResult InvokeDelegate(SyncRequestTask task, in ShortString messageName, in JsonValue messageValue, SyncContext syncContext) {
            var cmd     = new MessageContext(task, messageName,  syncContext);
            var param   = new Param<TValue> (messageValue, syncContext); 
            var result  = handler(param, cmd);
            
            if (!result.Success) {
                return new InvokeResult(result.error);
            }
            using (var pooled = syncContext.ObjectMapper.Get()) {
                var writer = pooled.instance.writer;
                writer.WriteNullMembers = result.writeNull;
                writer.Pretty           = result.writePretty;
                var jsonResult          = writer.WriteAsValue(result.value);
                return new InvokeResult(jsonResult);
            }
        }
    }
    
    // ----------------------------------- CommandDelegateAsync<,> -----------------------------------
    internal sealed class CommandDelegateAsync<TParam, TResult> : MessageDelegate
    {
        private  readonly   HostCommandHandlerAsync<TParam, TResult>    handler;

        internal override   MsgType                                     MsgType         => MsgType.Command;
        internal override   bool                                        IsSynchronous   => false;

        internal CommandDelegateAsync (string name, HostCommandHandlerAsync<TParam, TResult> handler) : base(name) {
            this.handler    = handler;
        }
        
        internal override async Task<InvokeResult> InvokeDelegateAsync(SyncRequestTask task, ShortString messageName, JsonValue messageValue, SyncContext syncContext) {
            var cmd     = new MessageContext(task, messageName,  syncContext);
            var param   = new Param<TParam> (messageValue, syncContext); 
            var result  = await handler(param, cmd).ConfigureAwait(false);
            
            if (!result.Success) {
                return new InvokeResult(result.error);
            }
            using (var pooled = syncContext.ObjectMapper.Get()) {
                var writer = pooled.instance;
                writer.WriteNullMembers = result.writeNull;
                writer.Pretty           = result.writePretty;
                var jsonResult          = writer.WriteAsValue(result.value);
                return new InvokeResult(jsonResult);
            }
        }
    }
}