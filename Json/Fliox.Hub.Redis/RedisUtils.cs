// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || REDIS

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using StackExchange.Redis;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.Redis
{
    internal static class RedisUtils
    {
        internal static IDatabase Database (SyncConnection connection, int dbIndex) {
            return connection.instance!.GetDatabase(dbIndex);
        }
        
        internal static HashEntry[] EntitiesToRedisEntries(List<JsonEntity> entities) {
            var count = entities.Count;
            var result = new HashEntry[count];
            for (int n = 0; n < count; n++) {
                var entity          = entities[n];
                RedisValue key      = KeyToRedisKey(entity.key);
                RedisValue value    = entity.value.AsReadOnlyMemory(); 
                result[n]           = new HashEntry(key, value);
            }
            return result;
        }
        
        internal static RedisValue[] EntitiesToRedisKeys(List<JsonEntity> entities) {
            var count = entities.Count;
            var result = new RedisValue[count];
            for (int n = 0; n < count; n++) {
                var key     = entities[n].key;
                result[n]   = KeyToRedisKey(key);
            }
            return result;
        }
        
        private static RedisValue KeyToRedisKey(in JsonKey key) {
            var encoding = key.GetEncoding();
            switch (encoding) {
                case JsonKeyEncoding.LONG:          return key.AsLong();
                case JsonKeyEncoding.NULL:          return default;
                case JsonKeyEncoding.GUID:          return key.AsString();  // todo avoid string creation
                case JsonKeyEncoding.STRING:        return key.AsString();  
                case JsonKeyEncoding.STRING_SHORT:  return key.AsString();  // todo avoid string creation
                default: throw new InvalidOperationException("unexpected");
            }
        }
        
        internal static RedisValue[] KeysToRedisKeys(ListOne<JsonKey> keys) {
            var count = keys.Count;
            var result = new RedisValue[count];
            for (int n = 0; n < count; n++) {
                result[n]   = KeyToRedisKey(keys[n]);
            }
            return result;
        }
        
        private static JsonKey KeyToJsonKey (in RedisValue key) {
            if (key.IsInteger) {
                key.TryParse(out long value);
                return new JsonKey(value);
            }
            // ReadOnlyMemory<byte> array = key; // uses RedisValue > implicit operator ReadOnlyMemory<byte>(RedisValue value)
            return new JsonKey(key.ToString()); // key is already a string -> used its reference
        }
        
        private static JsonValue ValueToJsonValue (in RedisValue value) {
            if (value.IsNull) {
                return default;
            }
            byte[] array = value;           // uses RedisValue > implicit operator byte[] (RedisValue value)
            return new JsonValue(array);    // JsonValue store UTF-8 byte arrays -> use conversion method above
        }
        
        internal static EntityValue[] KeyValuesToEntities(ListOne<JsonKey> ids, RedisValue[] values) {
            var count = values.Length;
            var result = new EntityValue[count];
            for (int n = 0; n < count; n++) {
                var value   = ValueToJsonValue(values[n]);
                result[n]   = new EntityValue(ids[n], value);
            }
            return result;
        }
        
        internal static EntityValue[] EntriesToEntities(HashEntry[] entries) {
            var count = entries.Length;
            var result = new EntityValue[count];
            for (int n = 0; n < count; n++) {
                var entry   = entries[n];
                var key     = KeyToJsonKey(entry.Name);
                var value   = ValueToJsonValue(entry.Value);
                result[n]   = new EntityValue(key, value);
            }
            return result;
        }
    }
}

#endif