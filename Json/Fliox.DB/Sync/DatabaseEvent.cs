// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.DB.Sync
{
    // ----------------------------------- event -----------------------------------
    public abstract class DatabaseEvent : DatabaseMessage {
        // note for all fields
        // used { get; set; } to force properties on the top of JSON
        
        /// Increasing event sequence number starting with 1.
        /// Each target (subscriber) has its own sequence.  
                                        public  int                 seq      { get; set; }
        /// The target the event is sent to
        [Fri.Property(Name = "target")] public  string              targetId { get; set; }
        /// The client which caused the event. Specifically the client which made a database change.
        [Fri.Property(Name = "client")] public  string              clientId { get; set; }
    }
   
}
