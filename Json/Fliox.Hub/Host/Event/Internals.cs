// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal readonly struct  SendEventsContext {
        internal  readonly  ObjectWriter        writer;
        /// <summary>Buffer for serialized <see cref="Protocol.SyncEvent"/>'s </summary>
        internal  readonly  List<JsonValue>     syncEvents;
        /// <summary>Buffer for serialized <see cref="Protocol.EventMessage"/>'s </summary>
        internal  readonly  List<JsonValue>     eventMessages;
        internal  readonly  bool                sendTargetClientId;
        
        internal SendEventsContext(
            ObjectWriter    writer,
            List<JsonValue> syncEvents,
            List<JsonValue> eventMessages,
            bool            sendTargetClientId)
        {
            this.writer             = writer;
            this.syncEvents         = syncEvents;
            this.eventMessages      = eventMessages;
            this.sendTargetClientId = sendTargetClientId;
        }
    }
}
