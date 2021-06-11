// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(ChangesEvent), Discriminant = "changes")]
    public abstract class DatabaseEvent {
        internal abstract   DatabaseEventType   EventType { get; }
    }

    public class ChangesEvent : DatabaseEvent
    {
        public              List<DatabaseTask>  tasks;
        
        internal override   DatabaseEventType   EventType => DatabaseEventType.changes;
    }
    
    
    // ReSharper disable InconsistentNaming
    public enum DatabaseEventType {
        changes
    }
}