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
    public abstract class EventHandler
    {
        public abstract void EnqueueEvent(FlioxClient client, EventMessage ev);
    }
    /// <summary>
    /// Creates a <see cref="EventHandler"/> using a <see cref="SynchronizationContext"/>
    /// The <see cref="SynchronizationContext"/> is required to ensure that <see cref="SubscriptionProcessor.OnEvent"/> is called on the
    /// same thread as all other methods calls of <see cref="FlioxClient"/> and <see cref="EntitySet{TKey,T}"/>.
    /// <para>
    ///   In case of UI applications like WinForms, WPF or Unity <see cref="SynchronizationContext.Current"/> can be used.
    /// </para> 
    /// <para>
    ///   In case of a Console application or a unit test where <see cref="SynchronizationContext.Current"/> is null
    ///   <see cref="SingleThreadSynchronizationContext"/> can be used.
    /// </para> 
    /// </summary>
    public class SynchronizedEventHandler : EventHandler
    {
        private readonly    SynchronizationContext              synchronizationContext;
        

        public SynchronizedEventHandler(SynchronizationContext synchronizationContext) {
            this.synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }
        
        public SynchronizedEventHandler() {
            synchronizationContext =
                SynchronizationContext.Current
                ?? throw new InvalidOperationException(SynchronizationContextIsNull);
        }
        
        private const string SynchronizationContextIsNull = @"SynchronizationContext.Current is null.
This is typically the case in console applications or unit tests. 
Consider running application / test withing SingleThreadSynchronizationContext.Run()";
        
        public override void EnqueueEvent(FlioxClient client, EventMessage ev) {
            synchronizationContext.Post(delegate {
                client._intern.subscriptionProcessor.OnEvent(client, ev);
            }, null);
        }
    }
    
    public class DirectEventHandler : EventHandler
    {
        public override void EnqueueEvent(FlioxClient client, EventMessage ev) {
            client._intern.subscriptionProcessor.OnEvent(client, ev);
        }
    }
    
    public class QueuingEventHandler : EventHandler
    {
        private readonly    ConcurrentQueue <QueuedMessage>      eventQueue = new ConcurrentQueue <QueuedMessage> ();

        /// <summary>
        /// Creates a queuing <see cref="EventHandler"/>.
        /// In this case the application must frequently call <see cref="ProcessEvents"/> to apply changes to the
        /// <see cref="FlioxClient"/>.
        /// This allows to specify the exact code point in an application (e.g. Unity) where <see cref="EventMessage"/>'s
        /// are applied to the <see cref="FlioxClient"/>.
        /// </summary>
        public QueuingEventHandler() { }
        
        public override void EnqueueEvent(FlioxClient client, EventMessage ev) {
            eventQueue.Enqueue(new QueuedMessage(client, ev));
        }
        
        /// <summary>
        /// Need to be called frequently by application to process subscription events.
        /// </summary>
        public void ProcessEvents() {
            while (eventQueue.TryDequeue(out QueuedMessage queuedMessage)) {
                var client  = queuedMessage.client;
                client._intern.subscriptionProcessor.OnEvent(client, queuedMessage.ev);
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