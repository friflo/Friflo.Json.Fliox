// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Utils;
using static System.Diagnostics.DebuggerBrowsableState;


namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal struct ClientIntern
    {
        private     FlioxClient                     client;
        
        // --- readonly / private - owned
        private     ObjectDiffer                    objectDiffer;       // create on demand
        private     JsonMergeWriter                 mergeWriter;        // create on demand
        private     EntityProcessor                 processor;          // create on demand
        private     ObjectMapper                    objectMapper;       // create on demand
        private     ReaderPool                      eventReaderPool;    // create on demand

        private     Dictionary<ShortString, Set>    setByName;
        internal    Dictionary<ShortString, Set>    SetByName => setByName ??= new Dictionary<ShortString, Set>(ShortString.Equality);
        
        [DebuggerBrowsable(Never)]
        internal    Dictionary<ShortString, MessageSubscriber>  subscriptions;          // create on demand - only used for subscriptions
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private     IReadOnlyCollection<MessageSubscriber>      Subscriptions => subscriptions?.Values;

        internal    List<MessageSubscriber>                     subscriptionsPrefix;    // create on demand - only used for subscriptions

        // --- mutable state
        internal    SyncStore                       syncStore;
        internal    EventProcessor                  eventProcessor;         // never null
        private     SubscriptionProcessor           subscriptionProcessor;  // lazy creation. Needed only if dealing with subscriptions 
        internal    ChangeSubscriptionHandler       changeSubscriptionHandler;
        internal    SubscriptionEventHandler        subscriptionEventHandler;
        internal    bool                            disposed;
        internal    int                             lastEventSeq;
        internal    int                             syncCount;
        internal    Timer                           ackTimer;
        internal    bool                            ackTimerPending;
        internal    ShortString                     userId;
        internal    ShortString                     clientId;
        internal    ShortString                     token;

        // --- usage: internal - SyncContext / SyncRequest
        internal    InstanceBuffer<SyncContext>     syncContextBuffer;
        internal    InstanceBuffer<SyncRequest>     syncRequestBuffer;
        internal    InstanceBuffer<MemoryBuffer>    memoryBufferPool;
        internal    InstanceBuffer<SyncStore>       syncStoreBuffer;
        // --- usage: API
        internal    InstanceBuffer<SyncResult>      syncResultBuffer;

        private static readonly SynchronousEventProcessor           DefaultEventProcessor   = new SynchronousEventProcessor();

        // --- create expensive / infrequently used objects on demand. Used method to avoid creation by debugger
        internal EntityProcessor        EntityProcessor()       => processor        ??= new EntityProcessor();
        internal ObjectDiffer           ObjectDiffer()          => objectDiffer     ??= new ObjectDiffer    (client._readonly.typeStore);
        internal JsonMergeWriter        JsonMergeWriter()       => mergeWriter      ??= new JsonMergeWriter (client._readonly.typeStore);
        internal ObjectMapper           ObjectMapper()          => objectMapper     ??= new ObjectMapper    (client._readonly.typeStore);
        internal ReaderPool             EventReaderPool()       => eventReaderPool  ??= new ReaderPool      (client._readonly.typeStore);

        internal SubscriptionProcessor  SubscriptionProcessor() => subscriptionProcessor ??= new SubscriptionProcessor();

        public   override string        ToString()              => "";

        internal void Init(FlioxClient client) {
            this.client     = client;
            syncStore       = new SyncStore();
            eventProcessor  = DefaultEventProcessor;
        }

        /// <summary>
        /// Set a custom <see cref="SubscriptionProcessor"/> to process subscribed database changes or messages (commands).<br/>
        /// E.g. notifying other application modules about created, updated, deleted or patches entities.
        /// To subscribe to database change events use <see cref="EntitySet{TKey,T}.SubscribeChanges"/>.
        /// To subscribe to message events use <see cref="SubscribeMessage"/>.
        /// </summary>
        [Obsolete]
        internal void SetSubscriptionProcessor(SubscriptionProcessor processor) {
            subscriptionProcessor?.Dispose();
            subscriptionProcessor = processor;
        }
        
        internal void Dispose() {
            // readonly - owned
            ackTimer?.Dispose();
            lock (client._readonly.pendingSyncs) { client._readonly.pendingSyncs.Clear(); }
            disposed = true;
            // messageReader.Dispose();
            subscriptionProcessor?.Dispose();
            subscriptionsPrefix?.Clear();
            subscriptions?.Clear();
            client._readonly.hub.RemoveEventReceiver(clientId);
            setByName?.Clear();
            processor?.Dispose();
            objectDiffer?.Dispose();
            mergeWriter?.Dispose();
            objectMapper?.Dispose();
        }
        
        internal void Reset () {
            client._readonly.hub.RemoveEventReceiver(clientId);
            userId          = default;
            clientId        = default;
            token           = default;
            lastEventSeq    = 0;
            syncCount       = 0;
            subscriptionsPrefix?.Clear();   // todo should assert if having open subscriptions 
            subscriptions?.Clear();         // todo should assert if having open subscriptions
            syncStore       = new SyncStore();
        }
        
        internal SubscribeMessageTask AddCallbackHandler(string name, MessageCallback handler) {
            var task = new SubscribeMessageTask(name, null);
            var subs = subscriptions;
            if (subs == null) {
                subs = subscriptions = new Dictionary<ShortString, MessageSubscriber>(ShortString.Equality);
            }
            var nameShort = new ShortString(name);
            if (!subs.TryGetValue(nameShort, out var subscriber)) {
                subscriber = new MessageSubscriber(name);
                subs.Add(nameShort, subscriber);
            } else {
                task.state.Executed = true;
            }
            if (subscriber.isPrefix) {
                if (subscriptionsPrefix == null) subscriptionsPrefix = new List<MessageSubscriber>();
                subscriptionsPrefix.Add(subscriber);
            }
            subscriber.callbackHandlers.Add(handler);
            return task;
        }
        
        internal SubscribeMessageTask RemoveCallbackHandler (string name, object handler) {
            var prefix      = SubscribeMessage.GetPrefix(name);
            var subsPrefix  = subscriptionsPrefix;
            if (!prefix.IsNull() && subsPrefix != null) {
                if (handler == null) {
                    subsPrefix.RemoveAll((sub) => sub.name.IsEqual(prefix));
                } else {
                    foreach (var sub in subsPrefix.Where(sub => sub.name.IsEqual(prefix))) {
                        sub.callbackHandlers.RemoveAll(callback => callback.HasHandler(handler));
                    }
                }
            }
            var nameShort   = new ShortString(name);
            var task        = new SubscribeMessageTask(name, true);
            var subs        = subscriptions;
            if (subs == null || !subs.TryGetValue(nameShort, out var subscriber)) {
                task.state.Executed = true;
                return task;
            }
            if (handler != null) {
                subscriber.callbackHandlers.RemoveAll((h) => h.HasHandler(handler));
            } else {
                subscriber.callbackHandlers.Clear();
            }
            if (subscriber.callbackHandlers.Count == 0) {
                subs.Remove(nameShort);
            } else {
                task.state.Executed = true;
            }
            return task;
        }
    }
}