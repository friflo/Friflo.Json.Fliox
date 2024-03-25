// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host.Event;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

namespace Friflo.Json.Fliox.Hub.Client
{
    public struct ClientOptions
    {
        // --- public
        
        /// <summary> If true the serialization of entities to JSON is prettified </summary>
        public  bool            WritePretty         { get => writePretty; set => SetWritePretty(value); }

        /// <summary> If true the serialization of entities to JSON write null fields. Otherwise null fields are omitted </summary>        
        public  bool            WriteNull           { get => writeNull;   set => SetWriteNull(value); }
        
        /// <summary>
        /// An <see cref="IEventReceiver"/> send subscribed events to a <see cref="FlioxClient"/> instance.<br/>
        /// Its its currently only used for testing.<br/>
        /// </summary>
        /// <remarks>
        /// <b>Note</b><br/>
        /// It must be set before calling <see cref="FlioxClient.SyncTasks"/> or assigning <see cref="FlioxClient.ClientId"/>.<br/>
        /// An <see cref="IEventReceiver"/> is registered by an <see cref="FlioxClient.ClientId"/>.<br/>
        /// Changing the id would result in not receiving events a client subscribed with an old id.<br/>
        /// </remarks>
        public  IEventReceiver  DebugEventReceiver  { private get => eventReceiver; set => SetEventReceiver(value); }
        
        public  bool            DebugReadObjects    { get; set; }

        // --- private
        [Browse(Never)] internal    IEventReceiver  eventReceiver;
        [Browse(Never)] internal    FlioxClient     client;
        [Browse(Never)] internal    bool            writePretty;
        [Browse(Never)] internal    bool            writeNull;
        
        private void SetWritePretty (bool value) {
            writePretty = value;
            foreach (var set in client.entitySets) {
                if (set == null) continue;
                set.WritePretty = value;
            }
        }

        private void SetWriteNull (bool value){
            writeNull = value;
            foreach (var set in client.entitySets) {
                if (set == null) continue;
                set.WriteNull = value;
            }
        }
        
        private void SetEventReceiver(IEventReceiver receiver)
        {
            if (!client._intern.clientId.IsNull()) {
                throw new InvalidOperationException($"cannot change EventReceiver after assigning {nameof(FlioxClient.ClientId)}");
            }
            if (client.GetSyncCount() > 0) {
                throw new InvalidOperationException($"cannot change EventReceiver after calling {nameof(FlioxClient.SyncTasks)}()");
            }
            if (!client._readonly.hub.SupportPushEvents) {
                throw new InvalidOperationException("used hub does not SupportPushEvents");
            }
            eventReceiver = receiver;
        }
    }
}