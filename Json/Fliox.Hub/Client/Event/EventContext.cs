// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Protocol;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable ReturnTypeCanBeEnumerable.Global
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Defines the signature of the event handler passed to <see cref="FlioxClient.SubscriptionEventHandler"/> <br/>
    /// </summary>
    /// <remarks>
    /// All subscription handler methods are synchronous by intention.<br/>
    /// <b>Reason:</b>
    /// In contrast to handler methods of a service or a web server subscription handlers don't return a result. <br/>
    /// In case the application need to call an asynchronous method consider using the approach below:
    /// <code>
    ///     Task.Factory.StartNew(() => AsyncMethod());
    /// </code>
    /// <b>Note:</b> exceptions thrown in the <c>AsyncMethod()</c> are unhandled. Add try/catch to log exceptions.
    /// </remarks>
    public delegate void SubscriptionEventHandler (EventContext context);
    
    /// <summary>
    /// The <see cref="EventContext"/> provide all information of subscription events received by a <see cref="FlioxClient"/>.<br/>
    /// </summary>
    /// <remarks>
    /// Subscription events are received by a client in case the client setup subscriptions by the <b>Subscribe*()</b> methods
    /// of <see cref="FlioxClient"/> or <see cref="EntitySet{TKey,T}"/>.<br/>
    /// The event context provide the following event data.
    /// <list type="bullet">
    ///   <item> The <see cref="UserId"/> - the origin of the event</item>
    ///   <item> The <see cref="Messages"/> send by a user </item>
    ///   <item> The container <see cref="Changes"/> made by a user. <br/>
    ///   Use <see cref="GetChanges{TKey,T}"/> to get typed container changes.
    ///   </item>
    ///   <item> The <see cref="EventInfo"/> containing the number of messages and database changes </item>
    /// </list>
    /// Database change events are not automatically applied to a <see cref="FlioxClient"/>.<br/>
    /// To apply database change events to a <see cref="FlioxClient"/> call <see cref="ApplyChangesTo"/>.
    /// </remarks>
    public sealed class EventContext : ILogSource
    {
        /// <summary> user id sending the <see cref="Messages"/> and causing the <see cref="Changes"/>  </summary>
        public              ShortString             UserId          => syncEvent.usr;
        /// <summary> incrementing sequence number of a received event </summary>
        public              int                     EventSeq        => seq;
        /// <summary> number of received events </summary>
        public              int                     EventCount      => processor.EventCount;
        /// <summary> return the <see cref="Messages"/> sent by a user </summary>
        public              List<Message>           Messages        => processor.messages;
        /// <summary> <see cref="Changes"/> return the changes per database container.
        /// Use <see cref="GetChanges{TKey,T}"/> to get <b>strongly typed</b> container changes </summary>
        public              List<Changes>           Changes         => processor.contextChanges;
        /// <summary> return the number of <see cref="Messages"/> and <see cref="Changes"/> of the subscription event </summary>
        public              EventInfo               EventInfo       => syncEvent.GetEventInfo();
        /// <summary> is true if the client is the origin of the event </summary>
        public              bool                    IsOrigin        => syncEvent.clt.IsEqual(Client._intern.clientId);
        /// <summary> is true if the client is the origin of the event </summary>
        public              ShortString             SrcClient       => syncEvent.clt;
        /// <summary> is private to be exposed only in Debugger </summary>
        private             FlioxClient             Client          { get; set; }
        
        public  override    string                  ToString()      => $"source user: {syncEvent.usr}";
        
        [DebuggerBrowsable(Never)] public           IHubLogger              Logger => Client.Logger;
        [DebuggerBrowsable(Never)] private readonly SubscriptionProcessor   processor;
        [DebuggerBrowsable(Never)] private          SyncEvent               syncEvent;
        [DebuggerBrowsable(Never)] private          int                     seq;

        internal EventContext(SubscriptionProcessor processor) {
            this.processor  = processor;
        }
        
        internal void Init(FlioxClient client, in SyncEvent syncEvent, int seq) {
            Client          = client;
            this.syncEvent  = syncEvent;
            this.seq        = seq;
        }
        
        /// <summary>
        /// Give <b>strongly typed</b> access to the changes made to a container.
        /// The container is identified by the passed <paramref name="entitySet"/>.
        /// These changes contain the: <see cref="Changes{TKey,T}.Creates"/>, <see cref="Changes{TKey,T}.Upserts"/>,
        /// <see cref="Changes{TKey,T}.Deletes"/> and <see cref="Changes{TKey,T}.Patches"/> made to a container
        /// </summary>
        public Changes<TKey, T> GetChanges<TKey, T>(EntitySet<TKey, T> entitySet) where T : class {
            return (Changes<TKey, T>)processor.GetChanges(entitySet.GetInstance());
        }
        
        /// <summary> Apply all <see cref="Changes"/> of the <see cref="EventContext"/> the given <paramref name="client"/> </summary>
        public void ApplyChangesTo(FlioxClient client)
        {
            FlioxClient.AssertTrackEntities(client, nameof(ApplyChangesTo));
            foreach (var entityChanges in processor.contextChanges) {
                var container = entityChanges.ContainerShort;
                if (!client.TryGetSetByName(container, out var entitySet))
                    continue;
                entityChanges.ApplyChangesToInternal(entitySet);
            }
        }
    }
}