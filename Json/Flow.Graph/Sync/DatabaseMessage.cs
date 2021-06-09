// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(ChangesMessage),    Discriminant = "changes")]
    public abstract class DatabaseMessage {
        internal abstract   MessageType  MessageType { get; }
    }

    public class ChangesMessage : DatabaseMessage
    {
        public              List<DatabaseTask>          changes;
        
        internal override   MessageType  MessageType => MessageType.changes;
    }
    
    
    // ReSharper disable InconsistentNaming
    public enum MessageType {
        changes
    }
}