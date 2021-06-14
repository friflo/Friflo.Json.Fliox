// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Mapper;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Flow.Sync
{
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(ChangesEvent), Discriminant = "changes")]
    public abstract class DatabaseEvent {
        public              string              targetId {get; set;}
        public              string              clientId {get; set;}

        internal abstract   DatabaseEventType   EventType { get; }
    }
}
