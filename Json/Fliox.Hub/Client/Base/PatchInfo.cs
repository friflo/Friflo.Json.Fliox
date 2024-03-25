// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable ConvertToAutoProperty
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Contain the <see cref="merge"/> patch applied to an entity identified by its <see cref="id"/>
    /// </summary>
    public readonly struct EntityPatchInfo
    {
                                    private readonly    JsonKey     id;
        [DebuggerBrowsable(Never)]  private readonly    JsonValue   merge;
        
        internal EntityPatchInfo (in JsonKey id, in JsonValue merge) {
            this.id     = id;
            this.merge  = merge;
        }
    }

    /// <summary>
    /// Contain the <see cref="Merge"/> patch patches applied to an <see cref="Entity"/>
    /// </summary>
    public readonly struct EntityPatchInfo<TKey,T> where T : class {
        public              TKey                                    Key     => key;
        public              JsonValue                               Merge   => entityPatch;
        public              T                                       Entity  => entity;
        
        [DebuggerBrowsable(Never)] internal  readonly   JsonValue   entityPatch;
        [DebuggerBrowsable(Never)] private   readonly   TKey        key;
        [DebuggerBrowsable(Never)] private   readonly   T           entity;
        public    override  string                                  ToString()  => key.ToString();

        internal EntityPatchInfo (in JsonValue entityPatch, TKey key, T entity) {
            this.entityPatch    = entityPatch;
            this.key            = key;
            this.entity         = entity;
        }
    }
}