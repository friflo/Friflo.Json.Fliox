// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  local;
        private readonly    Dictionary<string, TestContainer>   testContainers  = new Dictionary<string, TestContainer>();
        public  readonly    Dictionary<string, string>          syncErrors      = new Dictionary<string, string>();
        
        
        public TestDatabase(EntityDatabase local) {
            this.local = local;
        }
        
        public void ClearErrors() {
            syncErrors.Clear();
            foreach (var pair in testContainers) {
                var container = pair.Value;
                container.readEntityErrors.Clear();
                container.readTaskErrors.Clear();
                container.writeErrors.Clear();
                container.queryErrors.Clear();
            }
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            if (TryGetContainer(name, out EntityContainer container)) {
                return container;
            }
            EntityContainer localContainer = local.GetOrCreateContainer(name);
            var testContainer = new TestContainer(name, this, localContainer);
            testContainers.Add(name, testContainer);
            return testContainer;
        }
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            foreach (var task in syncRequest.tasks) {
                if (task is Echo echo) {
                    if (!syncErrors.TryGetValue(echo.message, out string error))
                        continue;
                    switch (error) {
                        case Simulate.SyncError:
                            return new SyncResponse{error = new SyncError{message = "simulated SyncError"}};
                        case Simulate.SyncException:
                            throw new SimulationException ("simulated SyncException");
                    }
                }
            }
            var response = await base.ExecuteSync(syncRequest, syncContext);
            return response;
        }

        public TestContainer GetTestContainer(string name) {
            return (TestContainer) GetOrCreateContainer(name);
        }
    }
    
    /// <summary>
    /// Used to create all possible errors and exceptions which can be made by a <see cref="EntityContainer"/> implementation.
    /// These are:
    /// <para>1. A task error set to <see cref="ICommandResult.Error"/> in a <see cref="ICommandResult"/>.</para>
    /// <para>2. Exceptions thrown by a <see cref="EntityContainer"/> command by a buggy implementation.</para>
    /// <para>3. One or more <see cref="EntityError"/>'s added to a <see cref="TaskResult"/> entity error dictionary.</para>
    /// <br></br>
    /// Note: The <see cref="TestContainer"/> dont modify the underlying <see cref="local"/> <see cref="EntityContainer"/>
    /// to avoid side effects by error tests.
    /// </summary>
    public class TestContainer : EntityContainer
    {
        private readonly    EntityContainer local;
        public  readonly    Dictionary<string, Action<EntityValue>> readEntityErrors    = new Dictionary<string, Action<EntityValue>>();
        public  readonly    Dictionary<string, Func<CommandError>>  readTaskErrors      = new Dictionary<string, Func<CommandError>>();
        
        public  readonly    Dictionary<string, string>  writeErrors = new Dictionary<string, string>();
        public  readonly    Dictionary<string, string>  queryErrors = new Dictionary<string, string>();
        

        
        public  override    bool            Pretty       => local.Pretty;

        public TestContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }

        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            var error = SimulateWriteErrors(command.entities.Keys.ToHashSet(), out var errors);
            if (error != null)
                return Task.FromResult(new CreateEntitiesResult {Error = error});
            if (errors != null)
                return Task.FromResult(new CreateEntitiesResult {createErrors = errors});
            return Task.FromResult(new CreateEntitiesResult());
        }

        public override Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command, SyncContext syncContext) {
            var error = SimulateWriteErrors(command.entities.Keys.ToHashSet(), out var errors);
            if (error != null)
                return Task.FromResult(new UpdateEntitiesResult {Error = error});
            if (errors != null)
                return Task.FromResult(new UpdateEntitiesResult {updateErrors = errors});
            return Task.FromResult(new UpdateEntitiesResult());
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            var error = SimulateWriteErrors(command.ids, out var errors);
            if (error != null)
                return Task.FromResult(new DeleteEntitiesResult {Error = error});
            if (errors != null)
                return Task.FromResult(new DeleteEntitiesResult {deleteErrors = errors});
            return Task.FromResult(new DeleteEntitiesResult());
        }

        /// Validation of JSON entity values in result set is required, as this this implementation is able to
        /// simulate assign invalid JSON via .<see cref="SimulateReadErrors"/>.
        /// E.g. the invalid JSON value used for <see cref="TestStoreErrors.Article2JsonError"/> 
        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
            var result = await local.ReadEntities(command, syncContext).ConfigureAwait(false);
            var databaseError = SimulateReadErrors(result.entities);
            if (databaseError != null)
                result.Error = databaseError;
            result.ValidateEntities(local.name, syncContext);
            return result;
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            var result = await local.QueryEntities(command, syncContext).ConfigureAwait(false);
            var databaseError = SimulateReadErrors(result.entities);
            if (databaseError != null) {
                result.Error = databaseError;
                return result;
            }
            if (queryErrors.TryGetValue(command.filterLinq, out string filterLinq)) {
                switch (filterLinq) {
                    case Simulate.QueryTaskException:
                        throw new SimulationException("simulated query exception");
                    case Simulate.QueryTaskError:
                        return new QueryEntitiesResult {Error = new CommandError {message = "simulated query error"}};
                }
            }
            return result;
        }
        
        
        // --- simulate read/write error methods
        private CommandError SimulateReadErrors(Dictionary<string,EntityValue> entities) {
            foreach (var pair in readEntityErrors) {
                var id      = pair.Key;
                if (entities.TryGetValue(id, out EntityValue value)) {
                    var action = pair.Value;
                    action(value);
                }
            }
            foreach (var pair in readTaskErrors) {
                var id      = pair.Key;
                if (entities.TryGetValue(id, out EntityValue value)) {
                    var func = pair.Value;
                    return func();
                }
            }
            return null;
        }
        
        public EntityError EntityError(string id) {
            var error = new EntityError(EntityErrorType.ReadError, name, id, "simulated read entity error");
            return error;
        }
        
        private CommandError SimulateWriteErrors(HashSet<string> entities, out Dictionary<string, EntityError> errors) {
            errors = null;
            foreach (var errorPair in writeErrors) {
                var id = errorPair.Key;
                if (entities.Contains(id)) {
                    var error = errorPair.Value;
                    switch (error) {
                        case Simulate.WriteTaskException:
                            throw new SimulationException("simulated write task exception");
                        case Simulate.WriteTaskError:
                            return new CommandError {message = "simulated write task error"};
                        case Simulate.WriteEntityError:
                            if (errors == null)
                                errors = new Dictionary<string, EntityError>();
                            var entityError = new EntityError(EntityErrorType.WriteError, name, id, "simulated write entity error");
                            errors.Add(id, entityError);
                            break;
                    }
                }
            }
            return null;
        }
    }

    public static class Simulate
    {
        public const string WriteEntityError    = "WRITE-ENTITY-ERROR";

        public const string QueryTaskException  = "QUERY-TASK-EXCEPTION";
        public const string QueryTaskError      = "QUERY-TASK-ERROR";
        
        public const string WriteTaskException  = "WRITE-TASK-EXCEPTION";
        public const string WriteTaskError      = "WRITE-TASK-ERROR";
        
        public const string SyncError           = "SYNC-ERROR";
        public const string SyncException       = "SYNC-EXCEPTION";
    }    

    public class SimulationException : Exception {
        public SimulationException(string message) : base(message) { }
    }
}
