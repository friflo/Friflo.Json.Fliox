// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || REDIS

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using StackExchange.Redis;
using static Friflo.Json.Fliox.Hub.Redis.RedisUtils;

// ReSharper disable UseAwaitUsing
// ReSharper disable UseIndexFromEndExpression
namespace Friflo.Json.Fliox.Hub.Redis
{
    internal sealed class RedisHashContainer : EntityContainer
    {
        private  readonly   int             dbIndex;
        private  readonly   RedisKey        nameKey;
    //  private             bool            tableExists;
        public   override   bool            Pretty      { get; }

        internal RedisHashContainer(string name, RedisHashDatabase database, bool pretty)
            : base(name, database)
        {
            dbIndex = database.DbIndex;
            nameKey = new RedisKey(name);
            Pretty  = pretty;
        }
        
        // not applicable to Redis
        // private async Task<TaskExecuteError> EnsureContainerExists(SyncConnection connection) => return null;
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new CreateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.entities.Count == 0) {
                return new CreateEntitiesResult();
            }
            try {
                var db      = Database(connection, dbIndex);
                var keys    = EntitiesToRedisKeys(command.entities);
                // obviously not optimal.
                var values  = await db.HashGetAsync(nameKey, keys).ConfigureAwait(false);
                var count = 0;
                foreach (var value in values) {
                    count += value.IsNull ? 0 : 1;
                }
                if (count > 0) {
                    var msg = $"found exiting entities. Count: {values.Length}";
                    return new CreateEntitiesResult { Error = new TaskExecuteError(msg) };    
                }
                var entries = EntitiesToRedisEntries(command.entities);
                await db.HashSetAsync(nameKey, entries).ConfigureAwait(false);
                return new CreateEntitiesResult();
            }
            catch (RedisException e) {
                return new CreateEntitiesResult { Error = DatabaseError(e.Message) };
            }
        }
        
        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new UpsertEntitiesResult { Error = syncConnection.Error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            try {
                var db      = Database(connection, dbIndex);
                var entries = EntitiesToRedisEntries(command.entities);
                await db.HashSetAsync(nameKey, entries).ConfigureAwait(false);
                return new UpsertEntitiesResult();
            }
            catch (RedisException e) {
                return new UpsertEntitiesResult { Error = DatabaseError(e.Message) };
            }
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new ReadEntitiesResult { Error = syncConnection.Error };
            }
            try {
                var db          = Database(connection, dbIndex);
                var keys        = KeysToRedisKeys(command.ids);
                var values      = await db.HashGetAsync(nameKey, keys).ConfigureAwait(false);
                var entities    = KeyValuesToEntities(command.ids, values);
                return new ReadEntitiesResult { entities = new Entities(entities) };
            }
            catch (RedisException e) {
                return new ReadEntitiesResult { Error = DatabaseError(e.Message) };
            }
        }

        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new QueryEntitiesResult { Error = syncConnection.Error };
            }
            var filter  = command.GetFilter();
            var db      = Database(connection, dbIndex);
            try {
                if (filter.IsTrue) {
                    var entries     = await db.HashGetAllAsync(nameKey).ConfigureAwait(false);
                    var entities    = EntriesToEntities(entries);
                    return new QueryEntitiesResult { entities = new Entities(entities) };    
                }
                // var where   = filter.RedisFilter();
                return new QueryEntitiesResult { Error = NotImplemented("todo") }; // TODO
            }
            catch (RedisException e) {
                return new QueryEntitiesResult { Error = new TaskExecuteError(e.Message) };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new AggregateEntitiesResult { Error = syncConnection.Error };
            }
            var db = Database(connection, dbIndex);
            if (command.type == AggregateType.count) {
                try {
                    var filter  = command.GetFilter();
                    if (filter.IsTrue) {
                        var count = await db.HashLengthAsync(nameKey).ConfigureAwait(false);
                        return new AggregateEntitiesResult { value = count };
                    }
                    return new AggregateEntitiesResult { Error = NotImplemented("todo") }; // TODO
                }
                catch (RedisException e) {
                    return new AggregateEntitiesResult { Error = DatabaseError(e.Message) };
                }
            }
            return new AggregateEntitiesResult { Error = NotImplemented($"type: {command.type}") };
        }

        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new DeleteEntitiesResult { Error = syncConnection.Error };
            }
            try {
                var db = Database(connection, dbIndex);
                if (command.all == true) {
                    var keys = await db.HashKeysAsync(nameKey).ConfigureAwait(false);
                    await db.HashDeleteAsync(nameKey, keys).ConfigureAwait(false);
                } else {
                    var keys = KeysToRedisKeys(command.ids);
                    await db.HashDeleteAsync(nameKey, keys).ConfigureAwait(false);
                }
                return new DeleteEntitiesResult();
            }
            catch (RedisException e) {
                return new DeleteEntitiesResult { Error = DatabaseError(e.Message) };
            }
        }
        
        private static TaskExecuteError DatabaseError(string message) {
            return new TaskExecuteError(TaskErrorType.DatabaseError, message);
        }
    }
}

#endif