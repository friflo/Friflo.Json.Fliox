// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

// internal Event API => use specific namespace
namespace Friflo.Json.Fliox.Hub.Client.Event
{
    internal sealed class MessageSubscriber
    {
        internal readonly   bool                    isPrefix;
        internal readonly   ShortString             name;
        internal readonly   List<MessageCallback>   callbackHandlers = new List<MessageCallback>();

        public   override   string                  ToString() => FormatToString();

        internal MessageSubscriber (string name) {
            var prefix = SubscribeMessage.GetPrefix(name);
            isPrefix = !prefix.IsNull();
            if (isPrefix) {
                this.name = prefix;
            } else {
                this.name = new ShortString(name);
            }
        }
        
        private string FormatToString() {
            if (isPrefix)
                return $"{name.AsString()}*";
            return name.AsString();
        }
        
        internal void InvokeCallbacks(in InvokeContext invokeContext, EventContext context) {
            var tempHandlers = invokeContext.tempCallbackHandlers;
            tempHandlers.Clear();
            tempHandlers.AddRange(callbackHandlers);
            
            foreach (var callbackHandler in tempHandlers) {
                try {
                    callbackHandler.InvokeCallback(invokeContext, context);
                }
                catch (Exception e) {
                    var type = callbackHandler.GetType().Name;
                    var msg = $"{type} failed. name: {callbackHandler.name}";
                    context.Logger.Log(HubLog.Error, msg, e);
                    Debug.Fail($"{msg}, exception {e}");
                }
            }
        }
    }
    
    internal readonly struct InvokeContext
    {
        internal  readonly  ShortString             name;
        internal  readonly  JsonValue               param;
        internal  readonly  ObjectReader            reader;
        internal  readonly  List<MessageCallback>   tempCallbackHandlers;
        
        public    override  string          ToString() => $"{name}(param: {param.AsString()})";

        internal InvokeContext(in ShortString name, in JsonValue param, ObjectReader reader, List<MessageCallback> tempCallbackHandlers) {
            this.name                   = name;
            this.param                  = param;
            this.reader                 = reader;
            this.tempCallbackHandlers   = tempCallbackHandlers; 
        }
    }
    
    internal abstract class MessageCallback {
        internal readonly   string  name;
        private  readonly   object  handlerObject;
        
        internal            bool    HasHandler (object handler) {
            if (handler == handlerObject)
                return true;
            return false;
        }

        public   override   string  ToString()                  => name;

        internal abstract void InvokeCallback(in InvokeContext invokeContext, EventContext context);
        
        internal MessageCallback (string name, object handler) {
            this.name       = name;
            handlerObject   = handler;
        } 
    }
    
    internal sealed class NonGenericMessageCallback : MessageCallback
    {
        private  readonly   MessageSubscriptionHandler   handler;
        
        internal NonGenericMessageCallback (string name, MessageSubscriptionHandler handler) : base(name, handler) {
            this.handler = handler;
        }
        
        internal override void InvokeCallback(in InvokeContext invokeContext, EventContext context) {
            var msg = new Message(invokeContext);
            handler(msg, context);
        }
    }
    
    internal sealed class GenericMessageCallback<TMessage> : MessageCallback
    {
        private  readonly   MessageSubscriptionHandler<TMessage>   handler;
        
        internal GenericMessageCallback (string name, MessageSubscriptionHandler<TMessage> handler) : base(name, handler) {
            this.handler = handler;
        }
        
        internal override void InvokeCallback(in InvokeContext invokeContext, EventContext context) {
            var msg = new Message<TMessage>(invokeContext);
            handler(msg, context);
        }
    }
}