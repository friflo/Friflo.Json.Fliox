// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || REDIS

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using StackExchange.Redis;
using static Friflo.Json.Fliox.Hub.Redis.RedisUtils;

// ReSharper disable UseAwaitUsing
// ReSharper disable UseIndexFromEndExpression
namespace Friflo.Json.Fliox.Hub.Redis
{
    public sealed class RedisContainer : EntityContainer
    {
        private  readonly   int             databaseNumber;
        private             bool            tableExists;
        public   override   bool            Pretty      { get; }

        internal RedisContainer(string name, RedisDatabase database, bool pretty)
            : base(name, database)
        {
            databaseNumber  = database.databaseNumber;
            Pretty          = pretty;
        }
        
        // todo remove?
        private async Task<TaskExecuteError> EnsureContainerExists(SyncConnection connection) {
            return null;
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var connection = await syncContext.GetConnection().ConfigureAwait(false);
            if (connection.Failed) {
                return new CreateEntitiesResult { Error = connection.error };
            }
            var error = await EnsureContainerExists(connection).ConfigureAwait(false);
            if (error != null) {
                return new CreateEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new CreateEntitiesResult();
            }
            try {
                var db      = Database(connection, databaseNumber);
                var keys    = CreateKeys(command.entities);
                // obviously not optimal.
                var values  = await db.HashGetAsync(new RedisKey(name), keys).ConfigureAwait(false);
                var count = 0;
                foreach (var value in values) {
                    count += value.IsNull ? 0 : 1;
                }
                if (count > 0) {
                    var msg = $"found exiting entities. Count: {values.Length}";
                    return new CreateEntitiesResult { Error = new TaskExecuteError(msg) };    
                }
                var entries = CreateEntries(command.entities);
                await db.HashSetAsync(new RedisKey(name), entries).ConfigureAwait(false);
                return new CreateEntitiesResult();
            }
            catch (RedisException e) {
                return new CreateEntitiesResult { Error = DatabaseError(e.Message) };
            }
        }
        
        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var connection = await syncContext.GetConnection().ConfigureAwait(false);
            if (connection.Failed) {
                return new UpsertEntitiesResult { Error = connection.error };
            }
            var error = await EnsureContainerExists(connection).ConfigureAwait(false);
            if (error != null) {
                return new UpsertEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            try {
                var db      = Database(connection, databaseNumber);
                var entries = CreateEntries(command.entities);
                await db.HashSetAsync(new RedisKey(name), entries).ConfigureAwait(false);
                return new UpsertEntitiesResult();
            }
            catch (RedisException e) {
                return new UpsertEntitiesResult { Error = DatabaseError(e.Message) };
            }
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var connection = await syncContext.GetConnection().ConfigureAwait(false);
            if (connection.Failed) {
                return new ReadEntitiesResult { Error = connection.error };
            }
            try {
                var db      = Database(connection, databaseNumber);
                var keys    = CreateKeys(command.ids);
                var values  = await db.HashGetAsync(new RedisKey(name), keys).ConfigureAwait(false);
                var entities = RedisUtils.CreateEntities(keys, values);
                return new ReadEntitiesResult { entities = entities };
            }
            catch (RedisException e) {
                return new ReadEntitiesResult { Error = DatabaseError(e.Message) };
            }
        }

        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var connection  = await syncContext.GetConnection().ConfigureAwait(false);
            if (connection.Failed) {
                return new QueryEntitiesResult { Error = connection.error };
            }
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "TRUE" : filter.RedisFilter();
            var sql     = SQLUtils.QueryEntitiesSQL(command, name, where);
            try {
                using var cmd = Command(sql, connection);
                return await SQLUtils.QueryEntities(cmd, command, sql).ConfigureAwait(false);
            }
            catch (RedisException e) {
                return new QueryEntitiesResult { Error = new TaskExecuteError(e.Message), sql = sql };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var connection = await syncContext.GetConnection().ConfigureAwait(false);
            if (connection.Failed) {
                return new AggregateEntitiesResult { Error = connection.error };
            }
            var db = Database(connection, databaseNumber);
            if (command.type == AggregateType.count) {
                try {
                    var filter  = command.GetFilter();
                    if (filter.IsTrue) {
                        var count = await db.HashLengthAsync(new RedisKey(name)).ConfigureAwait(false);
                        return new AggregateEntitiesResult { value = count };
                    } else {
                        var where   = filter.IsTrue ? "" : $" WHERE {filter.RedisFilter()}";
                        var sql     = $"SELECT COUNT(*) from {name}{where}";

                        var result  = await Execute(connection, sql).ConfigureAwait(false);
                        if (result.Failed) { return new AggregateEntitiesResult { Error = result.error }; }
                        return new AggregateEntitiesResult { value = (long)result.value };
                    }
                }
                catch (RedisException e) {
                    return new AggregateEntitiesResult { Error = DatabaseError(e.Message) };
                }
            }
            return new AggregateEntitiesResult { Error = NotImplemented($"type: {command.type}") };
        }

        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var connection = await syncContext.GetConnection().ConfigureAwait(false);
            if (connection.Failed) {
                return new DeleteEntitiesResult { Error = connection.error };
            }
            var error = await EnsureContainerExists(connection).ConfigureAwait(false);
            if (error != null) {
                return new DeleteEntitiesResult { Error = error };
            }
            try {
                var db = Database(connection, databaseNumber);
                var nameKey = new RedisKey(name);
                if (command.all == true) {
                    var keys = await db.HashKeysAsync(nameKey).ConfigureAwait(false);
                    await db.HashDeleteAsync(nameKey, keys).ConfigureAwait(false);
                } else {
                    var keys = CreateKeys(command.ids);
                    await db.HashDeleteAsync(new RedisKey(name), keys).ConfigureAwait(false);
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