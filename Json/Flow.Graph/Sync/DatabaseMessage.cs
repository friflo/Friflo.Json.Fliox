// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(DatabaseMessage), Discriminant = "database")]
    public abstract class PushMessage {
        internal abstract   PushMessageType     MessageType { get; }
    }

    public class DatabaseMessage : PushMessage
    {
        public              List<DatabaseTask>  tasks;
        
        internal override   PushMessageType     MessageType => PushMessageType.database;
    }
    
    
    // ReSharper disable InconsistentNaming
    public enum PushMessageType {
        database
    }
}