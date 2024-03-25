// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable MergeConditionalExpression
namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    public readonly struct EntityValue
    {
        [Ignore]    public      readonly    JsonKey     key;
        [DebuggerBrowsable(Never)]     
        [Serialize] private     readonly    JsonValue   value;
        [DebuggerBrowsable(Never)]
        [Serialize] private     readonly    EntityError error;
        
        [Ignore]                public      JsonValue   Json        => error == null ? value : throw new EntityException(error);
        [Ignore]                public      EntityError Error       => error;

        public override         string      ToString()  => GetString();
        
        public static readonly  IComparer<EntityValue> Comparer = new EntityValueComparer();

        public EntityValue(in JsonKey key) {
            if (key.IsNull()) throw new ArgumentNullException(nameof(key));
            this.key    = key;
            value       = default;
            error       = null;
        }

        public EntityValue(in JsonKey key, in JsonValue json) {
            if (key.IsNull()) throw new ArgumentNullException(nameof(key));
            this.key    = key;
            value       = json;
            error       = null;
        }
        
        public EntityValue(in JsonKey key, EntityError error) {
            if (key.IsNull()) throw new ArgumentNullException(nameof(key));
            this.key    = key;
            this.error  = error;
            value       = default;
        }
        
        private string GetString() {
            if (error == null) {
                return $"{key}  {value}";
            }
            return $"{key}  {error.type}: {error.message}";
        }
    }
    
    internal sealed class EntityValueComparer : IComparer<EntityValue>
    {
        public int Compare(EntityValue x, EntityValue y) {
            return x.key.Compare(y.key);
        }
    }
    
    public readonly struct EntityObject
    {
        public  readonly    object      entity;

        public EntityObject(object entity) {
            this.entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }
    
    /// <summary>
    /// Contains an array of tuples: (<see cref="JsonKey"/>, <see cref="JsonValue"/>)<br/>
    /// </summary>
    /// <remarks>
    /// Two aspects of this class<br/>
    /// - avoid passing array parameters: <c>EntityValue[]</c><br/>
    /// - be prepared to support array of tuples: (<see cref="JsonKey"/>, <see cref="object"/>)
    /// </remarks>
    public readonly struct Entities
    {
        private readonly    object              items;
        
        public              EntityValue[]       Values  => (EntityValue[])      items;
        public              List<EntityObject>  Objects => (List<EntityObject>) items;
        public              int                 Length  => items is EntityValue[] values ? values.Length : ((List<EntityObject>)items).Count;
        internal            EntitiesType        Type    => items is List<EntityObject> ? EntitiesType.Objects : EntitiesType.Values;

        public override     string          ToString() => $"Length: {Length}, Type: {Type}";

        public  Entities (EntityValue[] values) {
            items = values;
        }
        
        public  Entities (List<EntityValue> values) {
            items = values.ToArray();
        }
        
        public  Entities (List<EntityObject> objects) {
            items = objects;
        }
    }
    
    /// <summary>The type entities are stored in <see cref="Entities"/></summary>
    public enum EntitiesType
    {
        /// <summary>Entities are stored as an <see cref="object"/>[] - an array of reference types</summary>
        Objects,
        /// <summary>Entities are stored as an <see cref="JsonValue"/>[]</summary>
        Values,
    }
}