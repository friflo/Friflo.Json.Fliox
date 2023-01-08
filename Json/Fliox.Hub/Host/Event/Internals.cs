// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal readonly struct  SendEventsContext {
        internal  readonly  ObjectWriter        writer;
        internal  readonly  List<JsonValue>     syncEvents;
        internal  readonly  List<JsonValue>     eventMessages;
        
        internal SendEventsContext(ObjectWriter writer, List<JsonValue> syncEvents, List<JsonValue> eventMessages) {
            this.writer         = writer;
            this.syncEvents     = syncEvents;
            this.eventMessages  = eventMessages;
        }
    }
}
