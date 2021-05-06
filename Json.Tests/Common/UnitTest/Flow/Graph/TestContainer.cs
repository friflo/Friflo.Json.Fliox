using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  local;
        
        public TestDatabase(EntityDatabase local) {
            this.local = local;
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            if (TryGetContainer(name, out EntityContainer container)) {
                return container;
            }
            EntityContainer localContainer = local.GetOrCreateContainer(name);
            return new TestContainer(name, this, localContainer);;
        }

        public TestContainer GetTestContainer(string name) {
            return (TestContainer) GetOrCreateContainer(name);
        }
    }
    
    public class TestContainer : EntityContainer
    {
        private readonly    EntityContainer local;
        public  readonly    Dictionary<string, string> readErrors  = new Dictionary<string, string>();
        public  readonly    Dictionary<string, string> writeErrors = new Dictionary<string, string>();
        public  readonly    HashSet<string>            queryErrors = new HashSet<string>();
        
        public  override    bool            Pretty       => local.Pretty;
        public  override    SyncContext     SyncContext  => local.SyncContext;

        public TestContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }

        public override async Task<CreateEntitiesResult>    CreateEntities  (CreateEntities command) {
            var result = await local.CreateEntities(command);
            SimulateWriteErrors(command.entities, result.errors);
            return result;
        }

        public override async Task<UpdateEntitiesResult>    UpdateEntities  (UpdateEntities command) {
            return await local.UpdateEntities(command);
        }

        public override async Task<ReadEntitiesResult>      ReadEntities    (ReadEntities command) {
            var result = await local.ReadEntities(command);
            SimulateReadErrors(result.entities);
            return result;
        }
        
        public override async Task<QueryEntitiesResult>     QueryEntities   (QueryEntities command) {
            var result = await local.QueryEntities(command);
            SimulateReadErrors(result.entities);
            if (queryErrors.Contains(command.filterLinq)) {
                throw new SimulationException("simulated query exception");
            }
            return result;
        }
        
        public override async Task<DeleteEntitiesResult>    DeleteEntities  (DeleteEntities command) {
            return await local.DeleteEntities(command);
        }
        
        // --- simulate read/write error methods
        private void SimulateReadErrors(Dictionary<string,EntityValue> entities) {
            foreach (var readPair in readErrors) {
                var id      = readPair.Key;
                if (entities.TryGetValue(id, out EntityValue value)) {
                    var payload = readPair.Value;
                    switch (payload) {
                        case "READ-ERROR":
                            value.SetJson("null");
                            var error = new EntityError(EntityErrorType.ReadError, name, id, "simulated read error");
                            value.SetError(error);
                            break;
                        case "READ-EXCEPTION":
                            throw new SimulationException("simulated EntityContainer read exception");
                        default:
                            value.SetJson(payload); // modify JSON
                            break;
                    }
                }
            }
        }
        
        private void SimulateWriteErrors(Dictionary<string, EntityValue> entities, Dictionary<string, EntityError> errors) {
            foreach (var writePair in writeErrors) {
                var id      = writePair.Key;
                if (entities.TryGetValue(id, out EntityValue value)) {
                    var payload = writePair.Value;
                    switch (payload) {
                        case "WRITE-ERROR":
                            var error = new EntityError(EntityErrorType.WriteError, name, id, "simulated write error");
                            errors.Add(id, error);
                            break;
                        case "WRITE-EXCEPTION":
                            throw new SimulationException("simulated EntityContainer write exception");
                        default:
                            value.SetJson(payload); // modify JSON
                            break;
                    }
                }
            }
        }
    }

    public class SimulationException : Exception {
        public SimulationException(string message) : base(message) { }
    }
}
