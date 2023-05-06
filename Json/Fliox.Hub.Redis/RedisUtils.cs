// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || REDIS

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using StackExchange.Redis;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.Redis
{
    public static class RedisUtils
    {
        internal static DbCommand Command (string sql, SyncConnection connection) {
            throw new NotImplementedException();
        }
        
        internal static IDatabase Database (SyncConnection connection) {
            var multiplexer = connection.instance as ConnectionMultiplexer;
            return multiplexer!.GetDatabase();
        }
        
        internal static Task<SQLResult> Execute(SyncConnection connection, string sql) {
            throw new NotImplementedException();
        }
        
        internal static HashEntry[] CreateEntries(List<JsonEntity> entities) {
            var count = entities.Count;
            var result = new HashEntry[count];
            for (int n = 0; n < count; n++) {
                var entity  = entities[n];
                var key     = entity.key.AsString();
                var value   = entity.value.AsString();
                // Note: Would be nice if RedisValue could be created from ReadOnlySpan<byte> 
                result[n]   = new HashEntry(new RedisValue(key), new RedisValue(value));
            }
            return result;
        }
        
        internal static RedisValue[] CreateKeys(List<JsonEntity> entities) {
            var count = entities.Count;
            var result = new RedisValue[count];
            for (int n = 0; n < count; n++) {
                var key     = entities[n].key.AsString();
                // Note: Would be nice if RedisValue could be created from ReadOnlySpan<byte> 
                result[n]   = new RedisValue(key);
            }
            return result;
        }
        
        internal static RedisValue[] CreateKeys(List<JsonKey> keys) {
            var count = keys.Count;
            var result = new RedisValue[count];
            for (int n = 0; n < count; n++) {
                var key     = keys[n].AsString();
                // Note: Would be nice if RedisValue could be created from ReadOnlySpan<byte> 
                result[n]   = new RedisValue(key);
            }
            return result;
        }
    }
}

#endif