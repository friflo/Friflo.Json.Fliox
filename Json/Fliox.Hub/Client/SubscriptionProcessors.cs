// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Threading;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Creates a <see cref="SubscriptionProcessor"/> using a <see cref="SynchronizationContext"/>
    /// The <see cref="SynchronizationContext"/> is required to ensure that <see cref="SubscriptionProcessor.ProcessEvent"/> is called on the
    /// same thread as all other methods calls of <see cref="FlioxClient"/> and <see cref="EntitySet{TKey,T}"/>.
    /// <para>
    ///   In case of UI applications like WinForms, WPF or Unity <see cref="SynchronizationContext.Current"/> can be used.
    /// </para> 
    /// <para>
    ///   In case of a Console application or a unit test where <see cref="SynchronizationContext.Current"/> is null
    ///   <see cref="SingleThreadSynchronizationContext"/> can be used.
    /// </para> 
    /// </summary>
    public class SynchronizedSubscriptionProcessor : SubscriptionProcessor
    {
        private readonly    SynchronizationContext              synchronizationContext;
        

        public SynchronizedSubscriptionProcessor(FlioxClient client, SynchronizationContext synchronizationContext)
            : base (client)
        {
            this.synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }
        
        public SynchronizedSubscriptionProcessor(FlioxClient client)
            : base (client)
        {
            synchronizationContext =
                SynchronizationContext.Current
                ?? throw new InvalidOperationException(SynchronizationContextIsNull);
        }
        
        private const string SynchronizationContextIsNull = @"SynchronizationContext.Current is null.
This is typically the case in console applications or unit tests. 
Consider running application / test withing SingleThreadSynchronizationContext.Run()";
        
        public override void EnqueueEvent(EventMessage ev) {
            synchronizationContext.Post(delegate {
                ProcessEvent(ev);
            }, null);
        }
    }
    
    public class QueuingSubscriptionProcessor : SubscriptionProcessor
    {
        private readonly    ConcurrentQueue <EventMessage>      eventQueue = new ConcurrentQueue <EventMessage> ();

        /// <summary>
        /// Creates a queuing <see cref="SubscriptionProcessor"/>.
        /// In this case the application must frequently call <see cref="ProcessEvents"/> to apply changes to the
        /// <see cref="FlioxClient"/>.
        /// This allows to specify the exact code point in an application (e.g. Unity) where <see cref="EventMessage"/>'s
        /// are applied to the <see cref="FlioxClient"/>.
        /// </summary>
        public QueuingSubscriptionProcessor(FlioxClient client)
            : base (client)
        { }
        
        public override void EnqueueEvent(EventMessage ev) {
            eventQueue.Enqueue(ev);
        }
        
        /// <summary>
        /// Need to be called frequently if <see cref="SubscriptionProcessor"/> is initialized without a <see cref="SynchronizationContext"/>.
        /// </summary>
        public void ProcessEvents() {
            while (eventQueue.TryDequeue(out EventMessage eventMessage)) {
                ProcessEvent(eventMessage);
            }
        }
    }
}