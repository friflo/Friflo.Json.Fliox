// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class MessageSubscriber
    {
        internal readonly List<MessageHandler> messageHandlers = new List<MessageHandler>();
        
        internal void InvokeMessageHandlers(ObjectReader reader, JsonValue messageValue) {
            foreach (var messageHandler in messageHandlers) {
                try {
                    messageHandler.InvokeMessageHandler(reader, messageValue);
                }
                catch (Exception e) {
                    Debug.Fail($"MessageHandler failed. type: {messageHandler.MessageType.Name}, exception: {e}");
                }
            }
        }
    }
    
    internal abstract class MessageHandler {
        private  readonly   object  handlerObject;
        
        internal            bool    HasHandler (object handler) => handler == handlerObject;
        internal abstract   Type    MessageType { get; } 
        
        internal abstract void InvokeMessageHandler(ObjectReader reader, JsonValue messageValue);
        
        internal MessageHandler (object handler) {
            handlerObject = handler;
        } 
    }
    
    internal class MessageHandler<TMessage> : MessageHandler
    {
        private  readonly   Handler<TMessage>   handler;
        internal override   Type                MessageType => typeof(TMessage);
        
        internal MessageHandler (Handler<TMessage> handler) : base(handler) {
            this.handler = handler;
        }
        
        internal override void InvokeMessageHandler(ObjectReader reader, JsonValue messageValue) {
            var ev = new MessageEvent<TMessage>(messageValue.json, reader);
            handler(ev);
        }
    }
}