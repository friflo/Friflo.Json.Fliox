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
        internal readonly List<MessageHandler> handlers = new List<MessageHandler>();
        
        internal void CallHandlers(ObjectReader reader, JsonValue messageValue) {
            foreach (var handler in handlers) {
                try {
                    handler.CallHandler(reader, messageValue);
                }
                catch (Exception e) {
                    Debug.Fail($"MessageHandler failed. type: {handler.MessageType.Name}, exception: {e}");
                }
            }
        }
    }
    
    internal abstract class MessageHandler {
        private  readonly   object  actionObject;
        
        internal            bool    HasAction (object action) => action == actionObject;
        internal abstract   Type    MessageType { get; } 
        
        internal abstract void CallHandler(ObjectReader reader, JsonValue messageValue);
        
        internal MessageHandler (object actionObject) {
            this.actionObject = actionObject;
        } 
    }
    
    internal class MessageHandler<TMessage> : MessageHandler
    {
        private  readonly   Action<TMessage>    action;
        internal override   Type                MessageType => typeof(TMessage);
        
        internal MessageHandler (Action<TMessage> action) : base(action) {
            this.action = action;
        }
        
        internal override void CallHandler(ObjectReader reader, JsonValue messageValue) {
            var message = reader.Read<TMessage>(messageValue.json);
            action(message);
        }
    }
}