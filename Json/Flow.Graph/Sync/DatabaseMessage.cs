// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(ChangeMessage),    Discriminant = "subscribe")]
    public abstract class DatabaseMessage {
        internal abstract   MessageType  MessageType { get; }
    }

    public class ChangeMessage : DatabaseMessage
    {
        public              List<DatabaseTask>          change;
        
        internal override   MessageType  MessageType => MessageType.change;
    }
    
    
    // ReSharper disable InconsistentNaming
    public enum MessageType {
        change
    }
}