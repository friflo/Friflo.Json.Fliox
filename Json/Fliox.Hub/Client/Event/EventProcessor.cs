// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Threading;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// An <see cref="IEventProcessor"/> is used to process subscription events subscribed by a <see cref="FlioxClient"/><br/>
    /// By default a <see cref="FlioxClient"/> uses a <see cref="DirectEventProcessor"/> to handle subscription events
    /// in the thread the events arrive.
    /// </summary>
    public interface IEventProcessor
    {
        void EnqueueEvent(FlioxClient client, EventMessage ev);
    }
    
    /// <summary>
    /// Handle subscription events in the thread a event message arrived. <br/>
    /// E.g. In case of a <see cref="System.Net.WebSockets.WebSocket"/> in the thread reading data from the WebSocket stream.
    /// </summary>
    public sealed class DirectEventProcessor : IEventProcessor
    {
        public void EnqueueEvent(FlioxClient client, EventMessage ev) {
            var processor = client._intern.SubscriptionProcessor(); 
            processor.ProcessEvent(client, ev);
        }
    }
    
    /// <summary>
    /// Creates a <see cref="IEventProcessor"/> for UI based applications having a <see cref="SynchronizationContext"/>
    /// to ensure event callbacks are invoked on the UI thread.
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
    public sealed class SynchronizationContextProcessor : IEventProcessor
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
        
        public void EnqueueEvent(FlioxClient client, EventMessage ev) {
            synchronizationContext.Post(delegate {
                var processor = client._intern.SubscriptionProcessor();
                processor.ProcessEvent(client, ev);
            }, null);
        }
    }
    
    /// <summary>
    /// Is a queuing <see cref="IEventProcessor"/> giving an application full control when event callback are invoked.
    /// </summary>
    /// <remarks>
    /// In this case the application must frequently call <see cref="ProcessEvents"/> to apply changes to the
    /// <see cref="FlioxClient"/>.
    /// This allows to specify the exact code point in an application (e.g. Unity) to call the handler
    /// methods of message and changes subscriptions.
    /// </remarks>
    public sealed class QueuingEventProcessor : IEventProcessor
    {
        private readonly    ConcurrentQueue <QueuedMessage>      eventQueue = new ConcurrentQueue <QueuedMessage> ();

        public QueuingEventProcessor() { }
        
        public void EnqueueEvent(FlioxClient client, EventMessage ev) {
            eventQueue.Enqueue(new QueuedMessage(client, ev));
        }
        
        /// <summary>
        /// Need to be called frequently by application to process subscription events.
        /// </summary>
        public void ProcessEvents() {
            while (eventQueue.TryDequeue(out QueuedMessage queuedMessage)) {
                var client      = queuedMessage.client;
                var processor   = client._intern.SubscriptionProcessor();
                processor.ProcessEvent(client, queuedMessage.ev);
            }
        }

        private readonly struct QueuedMessage
        {
            internal  readonly  FlioxClient     client;
            internal  readonly  EventMessage    ev;
            
            internal QueuedMessage(FlioxClient client, EventMessage  ev) {
                this.client = client;
                this.ev     = ev;
            }
        }
    }
}