// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Friflo.Json.Fliox.Hub.Threading;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// An <see cref="EventProcessor"/> is used to process subscription events subscribed by a <see cref="FlioxClient"/>
    /// </summary>
    /// <remarks>
    /// By default a <see cref="FlioxClient"/> uses a <see cref="DirectEventProcessor"/> to handle subscription events
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
    public sealed class DirectEventProcessor : EventProcessor
    {
        public override void EnqueueEvent(FlioxClient client, in JsonValue eventMessage) {
            client.ProcessEvents(eventMessage);
        }
    }
    
    /// <summary>
    /// An <see cref="EventProcessor"/> implementation used for UI based applications having a <see cref="SynchronizationContext"/>
    /// </summary>
    /// <remarks>
    /// The <see cref="SynchronizationContextProcessor"/> ensures that the handler methods passed to the <b>Subscribe*()</b> methods of
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
    public sealed class SynchronizationContextProcessor : EventProcessor
    {
        private  readonly   SynchronizationContext  synchronizationContext;
        

        public SynchronizationContextProcessor(SynchronizationContext synchronizationContext) {
            this.synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }
        
        public SynchronizationContextProcessor() {
            synchronizationContext =
                SynchronizationContext.Current
                ?? throw new InvalidOperationException(SynchronizationContextIsNull);
        }
        
        private const string SynchronizationContextIsNull = @"SynchronizationContext.Current is null.
This is typically the case in console applications or unit tests. 
Consider running application / test withing SingleThreadSynchronizationContext.Run()";
        
        public override void EnqueueEvent(FlioxClient client, in JsonValue eventMessage) {
            var ev = new JsonValue(eventMessage); // todo use MessageBufferQueue instead of creating a copy #RAW_EV
            synchronizationContext.Post(delegate {
                client.ProcessEvents(ev);
            }, null);
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
    public sealed class QueuingEventProcessor : EventProcessor
    {
        private readonly    ConcurrentQueue <QueuedMessage>      eventQueue = new ConcurrentQueue <QueuedMessage> ();

        public QueuingEventProcessor() { }
        
        public override void EnqueueEvent(FlioxClient client, in JsonValue eventMessage) {
            var ev = new JsonValue (eventMessage); // todo use MessageBufferQueue instead of creating a copy #RAW_EV
            eventQueue.Enqueue(new QueuedMessage(client, ev));
        }
        
        /// <summary>
        /// Need to be called frequently by application to process subscription events.
        /// </summary>
        public void ProcessEvents() {
            while (eventQueue.TryDequeue(out QueuedMessage queuedMessage)) {
                var client      = queuedMessage.client;
                client.ProcessEvents(queuedMessage.eventMessage);
            }
        }

        private readonly struct QueuedMessage
        {
            internal  readonly  FlioxClient     client;
            internal  readonly  JsonValue       eventMessage;
            
            internal QueuedMessage(FlioxClient client, in JsonValue eventMessage) {
                this.client         = client;
                this.eventMessage   = eventMessage;
            }
        }
    }
}