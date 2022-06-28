// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.Client;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.Hub.Host.Internal
{
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