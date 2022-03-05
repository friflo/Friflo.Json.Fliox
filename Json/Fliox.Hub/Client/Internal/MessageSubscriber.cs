// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal sealed class MessageSubscriber
    {
        internal readonly   bool                    isPrefix;
        internal readonly   string                  name;
        internal readonly   List<MessageCallback>   callbackHandlers = new List<MessageCallback>();

        public   override   string                  ToString() => name;

        internal MessageSubscriber (string name) {
            var prefix = SubscribeMessage.GetPrefix(name);
            isPrefix = prefix != null;
            if (isPrefix) {
                this.name = prefix;
            } else {
                this.name = name;
            }
        }
        
        internal void InvokeCallbacks(in InvokeContext invokeContext) {
            foreach (var callbackHandler in callbackHandlers) {
                try {
                    callbackHandler.InvokeCallback(invokeContext);
                }
                catch (Exception e) {
                    var type = callbackHandler.GetType().Name;
                    var msg = $"{type} failed. name: {callbackHandler.name}, exception: {e}";
                    Console.WriteLine(msg);
                    Debug.Fail(msg);
                }
            }
        }
    }
    
    internal readonly struct InvokeContext
    {
        internal  readonly  string          name;
        internal  readonly  JsonValue       param;
        internal  readonly  ObjectReader    reader;

        public    override  string          ToString() => $"{name}(param: {param.AsString()})";

        internal InvokeContext(string name, in JsonValue param, ObjectReader reader) {
            this.name   = name;
            this.param  = param;
            this.reader = reader;
        }
    }
    
    internal abstract class MessageCallback {
        internal readonly   string  name;
        private  readonly   object  handlerObject;
        
        internal            bool    HasHandler (object handler) => handler == handlerObject;
        public   override   string  ToString()                  => name;

        internal abstract void InvokeCallback(in InvokeContext invokeContext);
        
        internal MessageCallback (string name, object handler) {
            this.name       = name;
            handlerObject   = handler;
        } 
    }
    
    internal sealed class NonGenericMessageCallback : MessageCallback
    {
        private  readonly   MessageSubscription   handler;
        
        internal NonGenericMessageCallback (string name, MessageSubscription handler) : base(name, handler) {
            this.handler = handler;
        }
        
        internal override void InvokeCallback(in InvokeContext invokeContext) {
            var msg = new Message(invokeContext);
            handler(msg);
        }
    }
    
    internal sealed class GenericMessageCallback<TMessage> : MessageCallback
    {
        private  readonly   MessageSubscription<TMessage>   handler;
        
        internal GenericMessageCallback (string name, MessageSubscription<TMessage> handler) : base(name, handler) {
            this.handler = handler;
        }
        
        internal override void InvokeCallback(in InvokeContext invokeContext) {
            var msg = new Message<TMessage>(invokeContext);
            handler(msg);
        }
    }
}