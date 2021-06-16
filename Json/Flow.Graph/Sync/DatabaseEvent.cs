// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Mapper;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Flow.Sync
{
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(ChangeEvent), Discriminant = "change")]
    public abstract class DatabaseEvent {
        // used { get; set; } to force properties on the top of JSON
        [Fri.Property(Name = "seq")]    public  int     eventSeq { get; set; }
        [Fri.Property(Name = "target")] public  string  targetId { get; set; }
        [Fri.Property(Name = "client")] public  string  clientId { get; set; }

        internal abstract   DatabaseEventType   EventType { get; }
    }
}
