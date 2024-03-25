// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Fliox.Utils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// An <see cref="EventProcessor"/> is used to process subscription events subscribed by a <see cref="FlioxClient"/>
    /// </summary>
    /// <remarks>
    /// By default a <see cref="FlioxClient"/> uses a <see cref="SynchronousEventProcessor"/> to handle subscription events
    /// in the thread the events arrive.
    /// </remarks>
    public abstract class EventProcessor
    {
        public abstract void EnqueueEvent(FlioxClient client, in JsonValue eventMessage);
    }
    
    /// <summary>
    /// Handle subscription events in the thread a event message arrived.
    /// </summary>
    /// <remarks>
    /// E.g. In case of a <see cref="System.Net.WebSockets.WebSocket"/> in the thread reading data from the WebSocket stream.
    /// </remarks>
    public sealed class SynchronousEventProcessor : EventProcessor
    {
        public override void EnqueueEvent(FlioxClient client, in JsonValue eventMessage) {
            client.ProcessEvents(eventMessage);
        }
    }
    
    /// <summary>
    /// An <see cref="EventProcessor"/> implementation used for UI based applications having a <see cref="SynchronizationContext"/>
    /// </summary>
    /// <remarks>
    /// The <see cref="EventProcessorContext"/> ensures that the handler methods passed to the <b>Subscribe*()</b> methods of
    /// <see cref="FlioxClient"/> and <see cref="EntitySet{TKey,T}"/> are called on the on the thread associated with
    /// the <see cref="SynchronizationContext"/>
    /// <br/>
    /// Depending on the application type <b>SynchronizationContext.Current</b> is null or not-null
    /// <list type="bullet">
    ///   <item>
    ///     In case of UI applications like WinForms, WPF or Unity <see cref="SynchronizationContext.Current"/> is not-null.<br/>
    ///     These application types utilize <see cref="SynchronizationContext.Current"/> to enable calling all UI methods
    ///     on the UI thread.
    ///   </item> 
    ///   <item>
    ///     In case of unit-tests or Console / ASP.NET Core applications <see cref="SynchronizationContext.Current"/> is null.<br/>
    ///     One way to establish a <see cref="SynchronizationContext"/> in these scenarios is to execute the whole
    ///     application / unit test within the <b>Run()</b> method of <see cref="SingleThreadSynchronizationContext"/>
    ///   </item>
    /// </list>
    /// </remarks>
    public sealed class EventProcessorContext : EventProcessor
    {
        private  readonly   SynchronizationContext          synchronizationContext;
        private  readonly   MessageBufferQueue<FlioxClient> messageQueue    = new MessageBufferQueue<FlioxClient>();
        private  readonly   List<MessageItem<FlioxClient>>  messages        = new List<MessageItem<FlioxClient>>(); 
        

        public EventProcessorContext(SynchronizationContext synchronizationContext) {
            this.synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }
        
        public EventProcessorContext() {
            synchronizationContext =
                SynchronizationContext.Current
                ?? throw new InvalidOperationException(SynchronizationContextIsNull);
        }
        
        private const string SynchronizationContextIsNull = @"SynchronizationContext.Current is null.
This is typically the case in console applications or unit tests. 
Consider running application / test withing SingleThreadSynchronizationContext.Run()";
        
        public override void EnqueueEvent(FlioxClient client, in JsonValue eventMessage) {
            lock (messageQueue) {
                messageQueue.AddTail(eventMessage, client);
            }
            synchronizationContext.Post(ProcessEvents, null);
        }
        
        private void ProcessEvents(object obj) {
            lock (messageQueue) {
                messageQueue.DequeMessages(messages);
            }
            foreach (var message in messages) {
                message.meta.ProcessEvents(message.value);
            }
        } 
    }
    
    /// <summary>
    /// Is a queuing <see cref="EventProcessor"/> giving an application full control when event callback are invoked.
    /// </summary>
    /// <remarks>
    /// In this case the application must frequently call <see cref="ProcessEvents"/> to apply changes to the
    /// <see cref="FlioxClient"/>.
    /// This allows to specify the exact code point in an application (e.g. Unity) to call the handler
    /// methods of message and changes subscriptions.
    /// </remarks>
    public sealed class EventProcessorQueue : EventProcessor
    {
        private  readonly   MessageBufferQueue<FlioxClient> messageQueue    = new MessageBufferQueue<FlioxClient>();
        private  readonly   List<MessageItem<FlioxClient>>  messages        = new List<MessageItem<FlioxClient>>();
        private  readonly   Action                          receivedEvent;

        public EventProcessorQueue(Action receivedEvent = null) {
            this.receivedEvent = receivedEvent;
        }
        
        public override void EnqueueEvent(FlioxClient client, in JsonValue eventMessage) {
            lock (messageQueue) {
                messageQueue.AddTail(eventMessage, client);
            }
            receivedEvent?.Invoke();
        }
        
        /// <summary>
        /// Need to be called frequently by application to process subscription events.
        /// </summary>
        public void ProcessEvents() {
            lock (messageQueue) {
                messageQueue.DequeMessages(messages);
            }
            foreach (var message in messages) {
                var client = message.meta;
                client.ProcessEvents(message.value);
            }
        }
    }
}