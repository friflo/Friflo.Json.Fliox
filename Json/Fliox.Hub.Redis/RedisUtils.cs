// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || REDIS

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using StackExchange.Redis;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.Redis
{
    public static class RedisUtils
    {
        internal static DbCommand Command (string sql, SyncConnection connection) {
            throw new NotImplementedException();
        }
        
        internal static IDatabase Database (SyncConnection connection, int databaseNumber) {
            var multiplexer = connection.instance as ConnectionMultiplexer;
            return multiplexer!.GetDatabase(databaseNumber);
        }
        
        internal static Task<SQLResult> Execute(SyncConnection connection, string sql) {
            throw new NotImplementedException();
        }
        
        internal static HashEntry[] CreateEntries(List<JsonEntity> entities) {
            var count = entities.Count;
            var result = new HashEntry[count];
            for (int n = 0; n < count; n++) {
                var entity          = entities[n];
                RedisValue key      = ToRedisValue(entity.key);
                RedisValue value    = entity.value.AsReadOnlyMemory(); 
                result[n]           = new HashEntry(key, value);
            }
            return result;
        }
        
        internal static RedisValue[] CreateKeys(List<JsonEntity> entities) {
            var count = entities.Count;
            var result = new RedisValue[count];
            for (int n = 0; n < count; n++) {
                var key     = entities[n].key;
                result[n]   = ToRedisValue(key);
            }
            return result;
        }
        
        private static RedisValue ToRedisValue(in JsonKey key) {
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
        
        internal static RedisValue[] CreateKeys(List<JsonKey> keys) {
            var count = keys.Count;
            var result = new RedisValue[count];
            for (int n = 0; n < count; n++) {
                result[n]   = ToRedisValue(keys[n]);
            }
            return result;
        }
        
        private static JsonKey ToJsonKey (in RedisValue key) {
            if (key.IsInteger) {
                key.TryParse(out long value);
                return new JsonKey(value);
            }
            byte[] array    = key; // uses implicit operator byte[] (RedisValue value)
            Bytes bytes     = default;
            bytes.buffer    = array;
            bytes.end       = array.Length;
            return new JsonKey(bytes, default);
        }
        
        private static JsonValue ToJsonValue (in RedisValue value) {
            if (value.IsNull) {
                return default;
            }
            byte[] array = value; // uses implicit operator byte[] (RedisValue value)
            return new JsonValue(array);
        }
        
        internal static EntityValue[] CreateEntities(RedisValue[] keys, RedisValue[] values) {
            var count = values.Length;
            var result = new EntityValue[count];
            for (int n = 0; n < count; n++) {
                var key     = ToJsonKey(keys[n]);
                var value   = ToJsonValue(values[n]);
                result[n]   = new EntityValue(key, value);
            }
            return result;
        }
    }
}

#endif