// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs
{
    public class TestDatabase : EntityDatabase
    {
        private  readonly   EntityDatabase                      local;
        internal readonly   Dictionary<string, TestContainer>   testContainers  = new Dictionary<string, TestContainer>();
        
        public   override   string                              StorageType => "TestDatabase";

        public TestDatabase(EntityDatabase local)
            : base (local.name, null, null)
        {
            this.local = local;
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            if (TryGetContainer(name, out EntityContainer container)) {
                return container;
            }
            EntityContainer localContainer = local.GetOrCreateContainer(name);
            var testContainer = new TestContainer(name, database, localContainer);
            testContainers.Add(name, testContainer);
            return testContainer;
        }
    }
    
    
    public class TestDatabaseHub : FlioxHub
    {
        
        public  readonly    Dictionary<string, Func<ExecuteSyncResult>> syncErrors  = new Dictionary<string, Func<ExecuteSyncResult>>();
        private readonly    TestDatabase testDatabase;

        public TestDatabaseHub(EntityDatabase database, SharedEnv env, string hostName = null)
            : base(new TestDatabase (database), env, hostName)
        {
            testDatabase = (TestDatabase)this.database;
        }
        
        public void ClearErrors() {
            syncErrors.Clear();
            foreach (var pair in testDatabase.testContainers) {
                var container = pair.Value;
                container.readEntityErrors.Clear();
                container.missingResultErrors.Clear();
                container.readTaskErrors.Clear();
                container.writeEntityErrors.Clear();
                container.writeTaskErrors.Clear();
                container.queryErrors.Clear();
            }
        }

        public override async Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            foreach (var task in syncRequest.tasks) {
                if (task is SendCommand message) {
                    if (!syncErrors.TryGetValue(message.name, out var fcn))
                        continue;
                    var resp = fcn();
                    return resp;
                }
            }
            var response = await base.ExecuteSync(syncRequest, syncContext);
            foreach (var pair in testDatabase.testContainers) {
                TestContainer testContainer = pair.Value;
                if (!response.success.resultMap.TryGetValue(testContainer.name, out var result))
                    continue;
                var entities = result.entityMap;
                foreach (var id in testContainer.missingResultErrors) {
                    var key = new JsonKey(id);
                    if (entities.TryGetValue(key, out EntityValue _)) {
                        entities.Remove(key);
                    }
                }
            }
            return response;
        }
        
        public TestContainer GetTestContainer(string container) {
            return (TestContainer) testDatabase.GetOrCreateContainer(container);
        }
    }
    
    /// <summary>
    /// Used to create all possible errors and exceptions which can be made by a <see cref="EntityContainer"/> implementation.
    /// These are:
    /// <para>1. A task error set to <see cref="ICommandResult.Error"/> in a <see cref="ICommandResult"/>.</para>
    /// <para>2. Exceptions thrown by a <see cref="EntityContainer"/> command by a buggy implementation.</para>
    /// <para>3. One or more <see cref="ReadError"/>'s added to a <see cref="SyncTaskResult"/> entity error dictionary.</para>
    /// <br></br>
    /// Note: The <see cref="TestContainer"/> doesnt modify the underlying <see cref="local"/> <see cref="EntityContainer"/>
    /// to avoid side effects by error tests.
    /// </summary>
    public class TestContainer : EntityContainer
    {
        private readonly    EntityContainer local;
        public  readonly    Dictionary<string, Action<EntityValue>> readEntityErrors    = new Dictionary<string, Action<EntityValue>>();
        public  readonly    HashSet<string>                         missingResultErrors = new HashSet<string>();
        public  readonly    Dictionary<string, Func<CommandError>>  readTaskErrors      = new Dictionary<string, Func<CommandError>>();

        public  readonly    Dictionary<string, Func<EntityError>>   writeEntityErrors   = new Dictionary<string, Func<EntityError>>();
        public  readonly    Dictionary<string, Func<CommandError>>  writeTaskErrors     = new Dictionary<string, Func<CommandError>>();

        public  readonly    Dictionary<string, Func<QueryEntitiesResult>>  queryErrors  = new Dictionary<string,  Func<QueryEntitiesResult>>();
        

        
        public  override    bool            Pretty       => local.Pretty;

        public TestContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }

        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            var ids     = command.entities.Select(entity => entity.key);
            var error   = SimulateWriteErrors(ids, out var errors);
            if (error != null)
                return Task.FromResult(new CreateEntitiesResult { Error = error });
            if (errors != null)
                return Task.FromResult(new CreateEntitiesResult { errors = errors });
            return Task.FromResult(new CreateEntitiesResult());
        }

        public override Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, SyncContext syncContext) {
            var ids     = command.entities.Select(entity => entity.key);
            var error   = SimulateWriteErrors(ids, out var errors);
            if (error != null)
                return Task.FromResult(new UpsertEntitiesResult { Error = error });
            if (errors != null)
                return Task.FromResult(new UpsertEntitiesResult { errors = errors });
            return Task.FromResult(new UpsertEntitiesResult());
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            var error = SimulateWriteErrors(command.ids, out var errors);
            if (error != null)
                return Task.FromResult(new DeleteEntitiesResult { Error = error });
            if (errors != null)
                return Task.FromResult(new DeleteEntitiesResult { errors = errors });
            return Task.FromResult(new DeleteEntitiesResult());
        }

        /// Validation of JSON entity values in result set is required, as this this implementation is able to
        /// simulate assign invalid JSON via .<see cref="SimulateReadErrors"/>.
        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
            var result = await local.ReadEntities(command, syncContext).ConfigureAwait(false);
            var databaseError = SimulateReadErrors(result.entities);
            if (databaseError != null)
                result.Error = databaseError;
            result.ValidateEntities(local.name, command.keyName, syncContext);
            return result;
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            var result = await local.QueryEntities(command, syncContext).ConfigureAwait(false);
            var databaseError = SimulateReadErrors(result.entities);
            if (databaseError != null) {
                result.Error = databaseError;
                return result;
            }
            var linq = command.GetFilter().Linq;
            if (queryErrors.TryGetValue(linq, out var func)) {
                return func();
            }
            return result;
        }
        
        public override Task<AggregateEntitiesResult> AggregateEntities (AggregateEntities command, SyncContext syncContext) {
            throw new NotImplementedException();
        }
        
        
        // --- simulate read/write error methods
        private CommandError SimulateReadErrors(List<EntityValue> entities) {
            foreach (var pair in readEntityErrors) {
                var id      = new JsonKey(pair.Key);
                var value   = entities.Find(entity => entity.Key.IsEqual(id));
                if (value != null) {
                    var action = pair.Value;
                    action(value);
                }
            }
            foreach (var pair in readTaskErrors) {
                var id      = new JsonKey(pair.Key);
                var value   = entities.Find(entity => entity.Key.IsEqual(id));
                if (value != null) {
                    var func = pair.Value;
                    return func();
                }
            }
            return null;
        }
        
        public EntityError ReadError(string id) {
            var error = new EntityError(EntityErrorType.ReadError, name, new JsonKey(id), "simulated read entity error");
            return error;
        }
        
        public EntityError WriteError(string id) {
            var error = new EntityError(EntityErrorType.WriteError, name, new JsonKey(id), "simulated write entity error");
            return error;
        }
        
        private CommandError SimulateWriteErrors(IEnumerable<JsonKey> entities, out List<EntityError> errors) {
            errors = null;
            foreach (var pair in writeEntityErrors) {
                var id = new JsonKey(pair.Key);
                if (entities.Contains(id, JsonKey.Equality)) {
                    if (errors == null)
                        errors = new List<EntityError>();
                    var fcn = pair.Value;
                    var entityError = fcn();
                    errors.Add(entityError);
                }
            }
            foreach (var pair in writeTaskErrors) {
                var id = new JsonKey(pair.Key);
                if (entities.Contains(id, JsonKey.Equality)) {
                    var func = pair.Value;
                    return func();
                }
            }
            return null;
        }
    }

    public class SimulationException : Exception {
        public SimulationException(string message) : base(message) { }
    }
}
