// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable JoinNullCheckWithUsage
namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    // Could be struct but this would make changing fields & properties complex as their changes require to update
    // EntitySet<TKey,T>.peers.
    // The benefit of a struct is higher memory locality and reduced heap allocations as the memory for all peers
    // are entirely contained by EntitySet<TKey,T>.peers Dictionary<TKey,Peer<T>>.
    // In case entities are already tracked by EntitySet<TKey,T>.peers no Peer<T> is instantiated on the heap
    // neither Peer<T> is a class nor a struct.
    internal sealed class Peer<T> where T : class
    {
        internal  readonly      JsonKey         id;     // never null
        private                 T               entity; // can be null 
        internal                EntityError     error;
        internal                PeerState       state;

        [DebuggerBrowsable(Never)] internal T   PatchSource     { get; private set; }
        [DebuggerBrowsable(Never)] internal T   NextPatchSource { get; private set; }
        /// Using the the unchecked <see cref="NullableEntity"/> must be an exception. Use <see cref="Entity"/> by default.
        [DebuggerBrowsable(Never)] internal T   NullableEntity   => entity;
        [DebuggerBrowsable(Never)] internal T   Entity           => entity ?? throw new InvalidOperationException($"Caller ensure & expect entity not null. id: '{id}'");

        public   override       string          ToString() => id.AsString();
    //  public   override       string          ToString() => entity != null ? entity.ToString() : "null";
        
        internal Peer(T entity, in JsonKey id) {
            if (entity == null)
                throw new NullReferenceException($"entity must not be null. Type: {typeof(T)}");
            this.entity = entity;
            this.id     = id;
        }
        
        internal Peer(in JsonKey id) {
            if (id.IsNull())
                throw new NullReferenceException($"id must not be null. Type: {typeof(T)}");
            this.id = id;
        }

        internal void SetEntity(T entity) {
            if (entity == null)
                throw new InvalidOperationException("Expect entity not null");
            if (this.entity == null) {
                this.entity = entity;
                return;
            }
            if (this.entity != entity)
                throw new ArgumentException($"Entity is already tracked by another instance. id: '{id}'");
        }

        internal void SetPatchSource(T entity) {
            if (entity == null)
                throw new InvalidOperationException("SetPatchSource() - expect entity not null");
            PatchSource = entity;
        }
        
        internal void SetPatchSourceNull() {
            PatchSource = null;
        }
        
        internal void SetNextPatchSource(T entity) {
            if (entity == null)
                throw new InvalidOperationException("SetNextPatchSource() - expect entity not null");
            NextPatchSource = entity;
        }
        
        internal void SetNextPatchSourceNull() {
            NextPatchSource = null;
        }
    }
    
    [Flags]
    internal enum PeerState {
        None    = 0,
        Created = 1,
        Updated = 2
    }
}
