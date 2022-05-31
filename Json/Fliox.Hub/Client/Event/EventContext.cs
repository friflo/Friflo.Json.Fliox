// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable ReturnTypeCanBeEnumerable.Global
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public delegate void SubscriptionEventHandler (EventContext context);
    
    /// <summary>
    /// The <see cref="EventContext"/> provide all information of subscription events received by a <see cref="FlioxClient"/>.<br/>
    /// Subscription events are received by a client in case the client setup subscriptions by the <b>Subscribe*()</b> methods
    /// of <see cref="FlioxClient"/> or <see cref="EntitySet{TKey,T}"/>.<br/>
    /// The event context provide the following event data.
    /// <list type="bullet">
    ///   <item> The <see cref="SrcUserId"/> - the origin of the event</item>
    ///   <item> The <see cref="Messages"/> send by a user </item>
    ///   <item> The database <see cref="Changes"/> made by a user </item>
    ///   <item> The <see cref="EventInfo"/> containing the number of messages and database changes </item>
    /// </list>
    /// Database change events are not automatically applied to a <see cref="FlioxClient"/>.<br/>
    /// To apply database change events to a <see cref="FlioxClient"/> call <see cref="ApplyChangesTo"/>.
    /// </summary>
    public sealed class EventContext : ILogSource
    {
        /// <summary> user id sending the <see cref="Messages"/> and causing the <see cref="Changes"/>  </summary>
        public              JsonKey                 SrcUserId       => ev.srcUserId;
        public              int                     EventSequence   => processor.EventSequence;
        /// <summary> return the <see cref="Messages"/> sent by a user </summary>
        public              IReadOnlyList<Message>  Messages        => processor.messages;
        /// <summary> <see cref="Changes"/> return the changes per database container. <br/>
        /// Use <see cref="GetChanges{TKey,T}"/> to access specific container changes </summary>
        public              IReadOnlyList<Changes>  Changes         => processor.contextChanges;
        /// <summary> return the number of <see cref="Messages"/> and <see cref="Changes"/> of the subscription event </summary>
        public              EventInfo               EventInfo       { get; private set; }
        
        [DebuggerBrowsable(Never)]
        public              IHubLogger              Logger { get; }

        [DebuggerBrowsable(Never)]
        private readonly    SubscriptionProcessor   processor;
        
        [DebuggerBrowsable(Never)]
        private readonly    EventMessage            ev;

        public  override    string                  ToString()  => $"source user: {ev.srcUserId}";

        internal EventContext(SubscriptionProcessor processor, EventMessage ev, IHubLogger logger) {
            this.processor  = processor;
            this.ev         = ev;
            EventInfo       = ev.GetEventInfo();
            Logger          = logger;
        }
        
        public Changes<TKey, T> GetChanges<TKey, T>(EntitySet<TKey, T> entitySet) where T : class {
            return (Changes<TKey, T>)processor.GetChanges(entitySet);
        }
        
        /// <summary> Apply all <see cref="Changes"/> the given <paramref name="client"/> </summary>
        public void ApplyChangesTo(FlioxClient client)
        {
            foreach (var entityChanges in processor.contextChanges) {
                var entityType = entityChanges.GetEntityType();
                if (!client._intern.TryGetSetByType(entityType, out var entitySet))
                    continue;
                entityChanges.ApplyChangesTo(entitySet);
            }
        }
    }
}