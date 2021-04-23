// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.EntityGraph
{
    public class PeerNotAssignedException : Exception
    {
        public readonly Entity entity;
        
        public PeerNotAssignedException(string message, Entity entity)
            : base ($"{message} Type: {entity.GetType().Name} id: {entity.id}")
        {
            this.entity = entity;
        }
    }
    
    public class PeerNotSyncedException : Exception
    {
        public PeerNotSyncedException(string message) : base (message) { }
    }
}