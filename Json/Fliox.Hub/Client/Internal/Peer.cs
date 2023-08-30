// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
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
    internal sealed class Peer<TKey, T> where T : class
    {
        [DebuggerBrowsable(Never)]  internal  readonly  TKey            key;     // never null
                                    private             T               entity; // can be null 
                                    private             EntityError     error;
        [DebuggerBrowsable(Never)]  internal            PeerState       state;

        [DebuggerBrowsable(Never)]  private             JsonValue       patchSource;
        [DebuggerBrowsable(Never)]  internal            JsonValue       PatchSource     => patchSource;
        
        [DebuggerBrowsable(Never)]  private             JsonValue       nextPatchSource;
        [DebuggerBrowsable(Never)]  internal            JsonValue       NextPatchSource => nextPatchSource;
        /// Using the the unchecked <see cref="NullableEntity"/> must be an exception. Use <see cref="Entity"/> by default.
        [DebuggerBrowsable(Never)]  internal            T               NullableEntity  => entity;
        [DebuggerBrowsable(Never)]  internal            T               Entity          => entity ?? throw new InvalidOperationException($"Caller ensure & expect entity not null. id: '{key}'");

        public   override                               string          ToString()      => FormatToString();
        
        internal Peer(in TKey key) {
            this.key = key;
        }
        
        internal Peer(in TKey key, T entity) {
            if (entity == null)
                throw new NullReferenceException($"entity must not be null. Type: {typeof(T)}");
            this.key    = key;
            this.entity = entity;
        }
        
        internal static  readonly   KeyConverter<TKey>          KeyConvert      = KeyConverter.GetConverter<TKey>();
        
        private string FormatToString() {
            var sb = new StringBuilder();
            // alternatively: show entity .ToString() 
            // if (entity != null) sb.Append(entity) else sb.Append("null");
            sb.Append(key);
            bool isFirst = true;
            if (state != PeerState.None) {
                sb.Append("  (");
                sb.Append(state);
                isFirst = false;
            }
            if (error != null) {
                sb.Append(isFirst ? "  (": ", ");
                sb.Append(error.type);
                isFirst = false;
            }
            if (!isFirst) {
                sb.Append(')');
            }
            return sb.ToString();            
        }
        
        internal void SetEntity(T entity)
        {
            /* alternative solution: delete peers, in this case this check need to be enabled 
            if (entity == null)
                throw new InvalidOperationException("Expect entity not null"); */
            if (this.entity == null) {
                this.entity = entity;
                return;
            }
            if (entity == null) {
                this.entity = null;
                return;
            }
            if (entity != this.entity) throw new ArgumentException($"Entity is already tracked by another instance. id: '{key}'");
        }
        
        internal void SetError(EntityError error) {
            this.error = error;
        }

        internal void SetPatchSource(in JsonValue value) {
            if (value.IsNull())
                throw new InvalidOperationException("SetPatchSource() - expect value not null");
            JsonValue.Copy(ref patchSource, value); // reuse JsonValue.array if big enough
        }
        
        internal void SetPatchSourceNull() {
            patchSource = default;
        }
        
        internal void SetNextPatchSource(in JsonValue value) {
            if (value.IsNull())
                throw new InvalidOperationException("SetNextPatchSource() - expect value not null");
            JsonValue.Copy(ref nextPatchSource, value);
        }
        
        internal void SetNextPatchSourceNull() {
            nextPatchSource = default;
        }
    }
    
    [Flags]
    internal enum PeerState {
        None    = 0,
        Create  = 1,
        Upsert  = 2
    }
}
