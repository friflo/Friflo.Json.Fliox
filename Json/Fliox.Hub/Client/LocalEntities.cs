// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Provide access to entities tracked by an <see cref="EntitySet{TKey,T}"/>. <br/>
    /// An entity become tracked if the <see cref="EntitySet{TKey,T}"/> gets aware of an entity by following calls  
    /// <list type="bullet">
    ///   <item>.Create(), .CreateRange(), .Upsert(), .UpsertRange()</item>
    ///   <item>.Read().Find() or .FindRange()</item>
    ///   <item>.Query(), .QueryAll(), .QueryByFilter()</item>
    /// </list> 
    /// </summary>
    public class LocalEntities<TKey, T> where T : class
    // Note:
    // could implement IReadOnlyDictionary<TKey, T> - but disadvantages predominate. reasons:
    // - have to use blurry names: e.g. .Values instead of .Entities, .TryGetValue() instead of .TryGetEntity()
    // - "Find References" on .Keys, .Values, ... gets imprecise. E.g. "Find References" results include also non LocalEntities<,> hits
    {
        /// <summary> return number of tracked entities </summary>
        [DebuggerBrowsable(Never)]
        public              int                 Count       => GetCount();
        /// <summary> Return the keys of all tracked entities in the <see cref="EntitySet{TKey,T}"/> </summary>
        public              List<TKey>          Keys        => KeysToList();
        /// <summary> Return all tracked entities in the <see cref="EntitySet{TKey,T}"/> </summary>
        public              List<T>             Entities    => EntitiesToList();

        private  readonly   EntitySet<TKey, T>  entitySet;

        public   override   string              ToString() => $"{entitySet.name}: {GetCount()}";

        internal LocalEntities (EntitySet<TKey, T> entitySet) {
            this.entitySet  = entitySet;
        }
    
        /// <summary>
        /// Return true if the <see cref="EntitySet{TKey,T}"/> contains an entity with the passed <paramref name="key"/>
        /// </summary>
        public bool ContainsKey(TKey key) {
            var peers = entitySet.GetPeers();
            if (peers == null)
                return false;
            return peers.ContainsKey(key);
        }

        /// <summary>
        /// Get the <paramref name="entity"/> with the passed <paramref name="key"/> from the <see cref="EntitySet"/>. <br/>
        /// Return true if the <see cref="EntitySet{TKey,T}"/> contains an entity with the given key. Otherwise false.
        /// </summary>
        public bool TryGetEntity(TKey key, out T entity) {
            var peers = entitySet.GetPeers();
            if (peers != null && peers.TryGetValue(key, out Peer<T> peer)) {
                    entity = peer.NullableEntity;
                    return true;
            }
            entity = null;
            return false;
        }

        public T this[TKey key] { get {
            var peers = entitySet.GetPeers();
            if (peers != null && peers.TryGetValue(key, out Peer<T> peer)) {
                return peer.NullableEntity;
            }
            var msg = $"key {key} not found in {entitySet.name}.Local";
            throw new KeyNotFoundException(msg);
        } }

        
        private int GetCount() {
            var peers   = entitySet.GetPeers();
            if (peers == null)
                return 0;
            return peers.Count;
        }
        
        private List<TKey> KeysToList() {
            var peers   = entitySet.GetPeers();
            if (peers == null) {
                return new List<TKey>();
            }
            var result  = new List<TKey>(peers.Count);
            foreach (var pair in peers) {
                result.Add(pair.Key);
            }
            return result;
        }
        
        /// <summary>
        /// Return all tracked entities of the <see cref="EntitySet{TKey,T}"/>
        /// </summary>
        private List<T> EntitiesToList() {
            var peers   = entitySet.GetPeers();
            if (peers == null) {
                return new List<T>();
            }
            var result  = new List<T>(peers.Count);
            foreach (var pair in peers) {
                T entity = pair.Value.NullableEntity;
                result.Add(entity);
            }
            return result;
        }
    }
}