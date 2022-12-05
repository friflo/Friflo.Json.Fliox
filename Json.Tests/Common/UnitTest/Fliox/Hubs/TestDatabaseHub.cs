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
            : base (local.name.value, null, null)
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
                var result = response.success.FindContainer(testContainer.name);
                if (result == null)
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
    
    public abstract class SimValue
    {
        internal abstract EntityValue ToEntityValue(string container, JsonKey key);
    }
    
    public class SimJson : SimValue
    {
        private readonly string        value;
        
        internal SimJson(string value) { this.value = value; }
        
        internal override EntityValue ToEntityValue(string container, JsonKey key) { return new EntityValue(key, new JsonValue(value)); }
    }
    
    public class SimReadError : SimValue
    {
        internal override EntityValue ToEntityValue(string container, JsonKey key) {
            var error = new EntityError(EntityErrorType.ReadError, container, key, "simulated read entity error");
            return new EntityValue(key, error);
        }
    }
    
    public class SimWriteError
    {
        internal EntityError ToEntityError(string container, JsonKey key) {
            return new EntityError(EntityErrorType.WriteError, container, key, "simulated write entity error");
        }
    }
    
    /// <summary>
    /// Used to create all possible errors and exceptions which can be made by a <see cref="EntityContainer"/> implementation.
    /// These are:
    /// <para>1. A task error set to <see cref="ICommandResult.Error"/> in a <see cref="ICommandResult"/>.</para>
    /// <para>2. Exceptions thrown by a <see cref="EntityContainer"/> command by a buggy implementation.</para>
    /// <para>3. One or more <see cref="SimValue"/> entity errors</para>
    /// <para>4. One or more <see cref="SimWriteError"/> entity errors</para>
    /// <br></br>
    /// Note: The <see cref="TestContainer"/> doesnt modify the underlying <see cref="local"/> <see cref="EntityContainer"/>
    /// to avoid side effects by error tests.
    /// </summary>
    public class TestContainer : EntityContainer
    {
        private readonly    EntityContainer local;
        public  readonly    Dictionary<string, SimValue>            readEntityErrors    = new Dictionary<string, SimValue>();
        public  readonly    HashSet<string>                         missingResultErrors = new HashSet<string>();
        public  readonly    Dictionary<string, Func<CommandError>>  readTaskErrors      = new Dictionary<string, Func<CommandError>>();

        public  readonly    Dictionary<string, SimWriteError>       writeEntityErrors   = new Dictionary<string, SimWriteError>();
        public  readonly    Dictionary<string, Func<CommandError>>  writeTaskErrors     = new Dictionary<string, Func<CommandError>>();

        public  readonly    Dictionary<string, Func<QueryEntitiesResult>>  queryErrors  = new Dictionary<string,  Func<QueryEntitiesResult>>();
        

        
        public  override    bool            Pretty       => local.Pretty;

        public TestContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }

        public override Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var ids     = command.entities.Select(entity => entity.key);
            var error   = SimulateWriteErrors(ids, out var errors);
            if (error != null)
                return Task.FromResult(new CreateEntitiesResult { Error = error });
            if (errors != null)
                return Task.FromResult(new CreateEntitiesResult { errors = errors });
            return Task.FromResult(new CreateEntitiesResult());
        }

        public override Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var ids     = command.entities.Select(entity => entity.key);
            var error   = SimulateWriteErrors(ids, out var errors);
            if (error != null)
                return Task.FromResult(new UpsertEntitiesResult { Error = error });
            if (errors != null)
                return Task.FromResult(new UpsertEntitiesResult { errors = errors });
            return Task.FromResult(new UpsertEntitiesResult());
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var error = SimulateWriteErrors(command.ids, out var errors);
            if (error != null)
                return Task.FromResult(new DeleteEntitiesResult { Error = error });
            if (errors != null)
                return Task.FromResult(new DeleteEntitiesResult { errors = errors });
            return Task.FromResult(new DeleteEntitiesResult());
        }

        /// Validation of JSON entity values in result set is required, as this this implementation is able to
        /// simulate assign invalid JSON via .<see cref="SimulateReadErrors"/>.
        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var result = await local.ReadEntitiesAsync(command, syncContext).ConfigureAwait(false);
            var databaseError = SimulateReadErrors(result.entities);
            if (databaseError != null)
                result.Error = databaseError;
            result.ValidateEntities(local.name, command.keyName, syncContext);
            return result;
        }
        
        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var result = await local.QueryEntitiesAsync(command, syncContext).ConfigureAwait(false);
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
        
        public override Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            throw new NotImplementedException();
        }
        
        
        // --- simulate read/write error methods
        private CommandError SimulateReadErrors(EntityValue[] entities) {
            for (int n = 0; n < entities.Length; n++) {
                var entity  = entities[n];
                var id      = entity.key.AsString();
                if (!readEntityErrors.TryGetValue(id, out var value))
                    continue;
                entities[n] = value.ToEntityValue(name, entity.key);
            }
            for (int n = 0; n < entities.Length; n++) {
                var entity  = entities[n];
                var id      = entity.key.AsString();
                if (!readTaskErrors.TryGetValue(id, out var error))
                    continue;
                return error();
            }
            return null;
        }
        
        private CommandError SimulateWriteErrors(IEnumerable<JsonKey> entities, out List<EntityError> errors) {
            errors = null;
            foreach (var pair in writeEntityErrors) {
                var id = new JsonKey(pair.Key);
                if (entities.Contains(id, JsonKey.Equality)) {
                    if (errors == null)
                        errors = new List<EntityError>();
                    var entityError = pair.Value.ToEntityError(name, id);
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
