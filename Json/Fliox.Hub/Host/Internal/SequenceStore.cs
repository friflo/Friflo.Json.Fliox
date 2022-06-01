// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Mapper;
using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredMemberAttribute;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.Hub.Host.Internal
{
    // --- models
    public sealed class Sequence {
        [Fri.Key]       public  string  container;
        [Req]           public  long    autoId;
        [Fri.Property  (Name =        "_etag")]
                        public  string  etag;
    }
    
    public sealed class SequenceKeys {
        [Fri.Key]       public  Guid    token;  // secret to ensure the client has reserved the keys
        [Req]           public  string  container;
        [Req]           public  long    start;
        [Req]           public  int     count;
                        public  JsonKey user;   // to track back who reserved keys in case of abuse
    }

    public sealed class SequenceStore : FlioxClient
    {
        // --- containers
        [Fri.Property(Name =                             "_sequence")]  
        public readonly EntitySet <string, Sequence>       sequence;
        [Fri.Property(Name =                             "_sequenceKeys")]  
        public readonly EntitySet <Guid,   SequenceKeys>   sequenceKeys;
        
        public  SequenceStore(FlioxHub hub) : base(hub) { }
        
        // ReSharper disable once RedundantOverriddenMember
        // enable set breakpoint. Ensures also FlioxClient.Dispose is virtual
        public override void Dispose() { 
            base.Dispose();
        }
    }
}