// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Client;
using Req = Friflo.Json.Fliox.RequiredMemberAttribute;
using Key = Friflo.Json.Fliox.PrimaryKeyAttribute;
using Property = Friflo.Json.Fliox.PropertyMemberAttribute;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.Hub.Host.Internal
{
    // --- models
    public sealed class Sequence {
        [Key]           public  string  container;
        [Req]           public  long    autoId;
        [Property  (Name =             "_etag")]
                        public  string  etag;
    }
    
    public sealed class SequenceKeys {
        [Key]           public  Guid    token;  // secret to ensure the client has reserved the keys
        [Req]           public  string  container;
        [Req]           public  long    start;
        [Req]           public  int     count;
                        public  JsonKey user;   // to track back who reserved keys in case of abuse
    }

    public sealed class SequenceStore : FlioxClient
    {
        // --- containers
        [Property(Name =                                 "_sequence")]  
        public readonly EntitySet <string, Sequence>       sequence;
        [Property(Name =                                 "_sequenceKeys")]  
        public readonly EntitySet <Guid,   SequenceKeys>   sequenceKeys;
        
        public  SequenceStore(FlioxHub hub) : base(hub) { }
        
        // ReSharper disable once RedundantOverriddenMember
        // enable set breakpoint. Ensures also FlioxClient.Dispose is virtual
        public override void Dispose() { 
            base.Dispose();
        }
    }
}