// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.DB.Host
{
    // --- models
    public class Sequence {
        [Fri.Key]       public  string  container;
        [Fri.Required]  public  long    autoId;
        [Fri.Property  (Name =        "_etag")]
                        public  string  etag;
    }
    
    public class SequenceKeys {
        [Fri.Key]       public  Guid    token;  // secret to ensure the client has reserved the keys
        [Fri.Required]  public  string  container;
        [Fri.Required]  public  long    start;
        [Fri.Required]  public  int     count;
                        public  string  user;   // to track back who reserved keys in case of abuse
    }

    public class SequenceStore : EntityStore
    {
        [Fri.Property(Name =                             "_sequence")]  
        public readonly EntitySet <string, Sequence>       sequence;
        [Fri.Property(Name =                             "_sequenceKeys")]  
        public readonly EntitySet <Guid,   SequenceKeys>   sequenceKeys;
        
        public  SequenceStore(EntityDatabase database, TypeStore typeStore, string clientId)
            : base(database, typeStore, clientId) { }
        
        // ReSharper disable once RedundantOverriddenMember
        // enable set breakpoint. Ensures also EntityStore.Dispose is virtual
        public override void Dispose() { 
            base.Dispose();
        }
    }
}