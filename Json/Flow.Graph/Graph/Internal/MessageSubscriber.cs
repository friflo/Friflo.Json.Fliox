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
        private  readonly   string                  name;
        internal readonly   List<MessageHandler>    messageHandlers = new List<MessageHandler>();

        public   override   string                  ToString() => name;

        internal MessageSubscriber (string name) {
            this.name = name;
        }
        
        internal void InvokeMessageHandlers(ObjectReader reader, JsonValue messageValue) {
            foreach (var messageHandler in messageHandlers) {
                try {
                    messageHandler.InvokeMessageHandler(reader, messageValue);
                }
                catch (Exception e) {
                    var msg = $"MessageHandler failed. name: {messageHandler.name}, exception: {e}";
                    Console.WriteLine(msg);
                    Debug.Fail(msg);
                }
            }
        }
    }
    
    internal abstract class MessageHandler {
        internal readonly   string  name;
        private  readonly   object  handlerObject;
        
        internal            bool    HasHandler (object handler) => handler == handlerObject;
        public   override   string  ToString()                  => name;

        internal abstract void InvokeMessageHandler(ObjectReader reader, JsonValue messageValue);
        
        internal MessageHandler (string name, object handler) {
            this.name       = name;
            handlerObject   = handler;
        } 
    }
    
    internal class GenericHandler : MessageHandler
    {
        private  readonly   Handler   handler;
        
        internal GenericHandler (string name, Handler handler) : base(name, handler) {
            this.handler = handler;
        }
        
        internal override void InvokeMessageHandler(ObjectReader reader, JsonValue messageValue) {
            var msg = new Message(name, messageValue.json, reader);
            handler(msg);
        }
    }
    
    internal class MessageHandler<TValue> : MessageHandler
    {
        private  readonly   Handler<TValue>   handler;
        
        internal MessageHandler (string name, Handler<TValue> handler) : base(name, handler) {
            this.handler = handler;
        }
        
        internal override void InvokeMessageHandler(ObjectReader reader, JsonValue messageValue) {
            var msg = new Message<TValue>(name, messageValue.json, reader);
            handler(msg);
        }
    }
}