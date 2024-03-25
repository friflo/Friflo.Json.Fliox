// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public readonly struct Create<TKey, T>
    {
        public  readonly    TKey        key;
        public  readonly    T           entity;
        public  override    string      ToString() => key.ToString();

        internal Create (TKey key, T entity) {
            this.key    = key;
            this.entity = entity;
        }
    }
    
    public readonly struct Upsert<TKey, T>
    {
        public  readonly    TKey        key;
        public  readonly    T           entity;
        public  override    string      ToString() => key.ToString();
        
        internal Upsert (TKey key, T entity) {
            this.key    = key;
            this.entity = entity;
        }
    }
    
    public readonly struct Patch<TKey> {
        public  readonly    TKey        key;
        public  readonly    JsonValue   patch;
        public  override    string      ToString() => key.ToString();
        
        internal Patch(TKey key, JsonValue patch) {
            this.key        = key;
            this.patch      = patch;
        }
    }
    
    public readonly struct Delete<TKey>
    {
        public  readonly    TKey        key;
        public  override    string      ToString() => key.ToString();
        
        internal Delete (TKey key) {
            this.key    = key;
        }
    }
    
    public readonly struct RawChanges
    {
        public  readonly    List<JsonValue>    creates;
        public  readonly    List<JsonValue>    upserts;
        public  readonly    List<JsonKey>      deletes;
        public  readonly    List<JsonValue>    patches;
        
        internal RawChanges (object _) {
            creates = new List<JsonValue>();
            upserts = new List<JsonValue>();
            deletes = new List<JsonKey>();
            patches = new List<JsonValue>();
        }
    }
    
        
    [Flags]
    public enum ApplyInfoType {
        EntityCreated   = 0x01,
        EntityUpdated   = 0x02,
        EntityDeleted   = 0x04,
        EntityPatched   = 0x08,
        ParseError      = 0x80,
    }
    
    public readonly struct ApplyInfo<TKey, T> where T : class {
        public  readonly    ApplyInfoType   type;
        public  readonly    TKey            key;
        public  readonly    T               entity;
        public  readonly    JsonValue       rawEntity;

        public  override    string          ToString() => $"{type} key: {key}"; 

        internal ApplyInfo(ApplyInfoType type, TKey key, T entity, in JsonValue rawEntity) {
            this.type       = type;
            this.key        = key;
            this.entity     = entity;
            this.rawEntity  = rawEntity;
        }
    }

    public readonly struct ApplyResult<TKey, T> where T : class {
        public readonly List<ApplyInfo<TKey,T>> applyInfos;
        
        public override string  ToString() => applyInfos != null ? $"Count: {applyInfos.Count}" : "error";
        
        internal ApplyResult(List<ApplyInfo<TKey,T>> applyInfos) {
            this.applyInfos = applyInfos;
        }
    }
    
    internal class SubscriptionIntern
    {
        internal            ObjectMapper    objectMapper;
        internal  readonly  EntityProcessor entityProcessor;
        internal  readonly  List<JsonKey>   keys;
        
        internal SubscriptionIntern() {
            entityProcessor = new EntityProcessor();
            keys            = new List<JsonKey>();
        }
    }
}