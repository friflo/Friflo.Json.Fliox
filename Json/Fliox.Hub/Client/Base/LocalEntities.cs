// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Provide access to entities tracked by an <see cref="EntitySet{TKey,T}"/>.
    /// </summary>
    /// <remarks>
    /// An entity become tracked if the <see cref="EntitySet{TKey,T}"/> gets aware of an entity by following calls  
    /// <list type="bullet">
    ///   <item>.Create(), .CreateRange(), .Upsert(), .UpsertRange()</item>
    ///   <item>.Read().Find() or .FindRange()</item>
    ///   <item>.Query(), .QueryAll(), .QueryByFilter()</item>
    /// </list>
    /// <see cref="LocalEntities{TKey,T}"/> adapts the behavior of <see cref="IReadOnlyDictionary{TKey,TValue}"/>
    /// </remarks>
    /// <typeparam name="TKey">Entity key type</typeparam>
    /// <typeparam name="T">Entity type</typeparam>
    public sealed class LocalEntities<TKey, T> : IEnumerable<KeyValuePair<TKey, T>> where T : class
    // Note:
    // could implement IReadOnlyDictionary<TKey, T> - but disadvantages predominate. reasons:
    // - have to use blurry names: e.g. .Values instead of .Entities, .TryGetValue() instead of .TryGetEntity()
    // - "Find References" on .Keys, .Values, ... gets imprecise. E.g. "Find References" results include also non LocalEntities<,> hits
    {
    #region - members
        /// <summary> return number of tracked entities </summary>
        [DebuggerBrowsable(Never)]
        public              int             Count       => GetCount();
        /// <summary> Return the keys of all tracked entities in the <see cref="EntitySet{TKey,T}"/> </summary>
        public              TKey[]          Keys        => KeysToArray();
        /// <summary> Return all tracked entities in the <see cref="EntitySet{TKey,T}"/> </summary>
        public              T[]             Entities    => EntitiesToArray();

        private  readonly   Dictionary<TKey,Peer<TKey, T>>  peers;
        private  readonly   Set<TKey, T>                    set;
        
        public   override   string          ToString() => $"{set.name}: {GetCount()}";
        
        #endregion

    #region - initialize
        internal LocalEntities (Set<TKey, T> set) {
            peers       = set.GetPeers();
            this.set    = set;
        }
        #endregion
    
    #region - methods
        /// <summary>
        /// Return true if the <see cref="EntitySet{TKey,T}"/> contains an entity with the passed <paramref name="key"/>
        /// </summary>
        public bool ContainsKey(TKey key) {
            return peers.ContainsKey(key);
        }

        /// <summary>
        /// Get the <paramref name="entity"/> with the passed <paramref name="key"/> from the <see cref="Set"/>.
        /// Return true if the <see cref="EntitySet{TKey,T}"/> contains an entity with the given key. Otherwise false.
        /// </summary>
        public bool TryGetEntity(TKey key, out T entity) {
            if (peers.TryGetValue(key, out Peer<TKey, T> peer)) {
                entity = peer.NullableEntity;
                return true;
            }
            entity = null;
            return false;
        }

        /// <summary>
        /// Gets the tracked entity associated with the specified <paramref name="key"/>.
        /// </summary>
        public T this[TKey key] { get {
            if (peers.TryGetValue(key, out Peer<TKey, T> peer)) {
                return peer.NullableEntity;
            }
            var msg = $"key '{key}' not found in {set.name}.Local";
            throw new KeyNotFoundException(msg);
        } }
        
        
        /*
        public T GetByKey(in JsonKey key) {
            var peers   = set.GetPeers();
            var id      = KeyConvert.IdToKey(key);
            if (peers != null && peers.TryGetValue(id, out Peer<TKey, T> peer)) {
                return peer.NullableEntity;
            }
            var msg = $"key '{key}' not found in {set.name}.Local";
            throw new KeyNotFoundException(msg);
        }
        
        public bool TryGetEntityByKey(in JsonKey key, out T entity) {
            var peers   = set.GetPeers();
            var id      = KeyConvert.IdToKey(key);
            if (peers != null && peers.TryGetValue(id, out Peer<TKey, T> peer)) {
                entity = peer.NullableEntity;
                return true;
            }
            entity = null;
            return false;
        } */
        
        public void Add(T entity) {
            set.TrackEntity(entity, PeerState.None);
        }

        private int GetCount() {
            return peers.Count;
        }
        
        private TKey[] KeysToArray() {
            if (peers.Count == 0) {
                return Array.Empty<TKey>();
            }
            var result  = new TKey[peers.Count];
            int n       = 0;
            foreach (var pair in peers) {
                result[n++] = pair.Key;
            }
            return result;
        }
        
        private T[] EntitiesToArray() {
            if (peers.Count == 0) {
                return Array.Empty<T>();
            }
            var result  = new T[peers.Count];
            int n       = 0;
            foreach (var pair in peers) {
                T entity = pair.Value.NullableEntity;
                result[n++] = entity;
            }
            return result;
        }
        
        #endregion
        
    #region - IEnumerable<>
        /// <summary> Returns an enumerator that iterates through the <see cref="LocalEntities{TKey,T}"/> </summary>
        public IEnumerator<KeyValuePair<TKey, T>> GetEnumerator() {
            return new Enumerator(peers);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        #endregion
        
    #region ---- Enumerator ----
        private struct Enumerator : IEnumerator<KeyValuePair<TKey, T>>
        {
            private Dictionary<TKey,Peer<TKey, T>>.Enumerator enumerator;

            internal Enumerator(Dictionary<TKey, Peer<TKey, T>> peers) {
                enumerator  = peers.GetEnumerator();
            }

            public bool MoveNext() {
                return enumerator.MoveNext();
            }

            public void Reset() { throw new NotImplementedException(); }

            public KeyValuePair<TKey, T> Current { get {
                var current = enumerator.Current;
                var value   = current.Value;
                if (value == null) {
                    return new KeyValuePair<TKey, T>(current.Key, default);
                }
                return new KeyValuePair<TKey, T>(current.Key, value.NullableEntity);
            } }

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
        #endregion
    }
}