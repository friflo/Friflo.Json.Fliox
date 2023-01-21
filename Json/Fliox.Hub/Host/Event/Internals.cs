// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal readonly struct  SendEventsContext {
        internal  readonly  ObjectWriter        writer;
        /// <summary>Buffer for serialized <see cref="Protocol.SyncEvent"/>'s </summary>
        internal  readonly  List<JsonValue>     eventBuffer;
        /// <summary>Buffer for serialized <see cref="Protocol.EventMessage"/>'s </summary>
        internal  readonly  List<JsonValue>     eventMessages;
        internal  readonly  bool                sendTargetClientId;
        
        internal SendEventsContext(
            ObjectWriter    writer,
            List<JsonValue> eventBuffer,
            List<JsonValue> eventMessages,
            bool            sendTargetClientId)
        {
            this.writer             = writer;
            this.eventBuffer        = eventBuffer;
            this.eventMessages      = eventMessages;
            this.sendTargetClientId = sendTargetClientId;
        }
    }
    
    internal readonly struct  Event {
        internal  readonly  EventType   type;

        public    override  string      ToString() => type.ToString();

        internal Event(EventType type) {
            this.type = type;
        }
    }
    
    internal enum EventType {
        /** Identify a <see cref="SyncEvent"/> */       SyncEvent       = 1,
        /** Identify an <see cref="EventMessage"/> */   EventMessage    = 2
    }
}
