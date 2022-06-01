// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.Client;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.Hub.Host.Internal
{
    // --- models
    public sealed class Sequence {
        [Key]       public  string  container;
        [Required]  public  long    autoId;
        [Serialize(Name =         "_etag")]
                    public  string  etag;
    }
    
    public sealed class SequenceKeys {
        [Key]       public  Guid    token;  // secret to ensure the client has reserved the keys
        [Required]  public  string  container;
        [Required]  public  long    start;
        [Required]  public  int     count;
                    public  JsonKey user;   // to track back who reserved keys in case of abuse
    }

    public sealed class SequenceStore : FlioxClient
    {
        // --- containers
        [Serialize(Name =                                "_sequence")]  
        public readonly EntitySet <string, Sequence>       sequence;
        [Serialize(Name =                                "_sequenceKeys")]  
        public readonly EntitySet <Guid,   SequenceKeys>   sequenceKeys;
        
        public  SequenceStore(FlioxHub hub) : base(hub) { }
        
        // ReSharper disable once RedundantOverriddenMember
        // enable set breakpoint. Ensures also FlioxClient.Dispose is virtual
        public override void Dispose() { 
            base.Dispose();
        }
    }
}