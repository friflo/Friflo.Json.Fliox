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
        public  readonly    Dictionary<string, string> readError  = new Dictionary<string, string>();  
        public  readonly    Dictionary<string, string> writeError = new Dictionary<string, string>();
        
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
            return result;
        }
        
        public override async Task<DeleteEntitiesResult>    DeleteEntities  (DeleteEntities command) {
            return await local.DeleteEntities(command);
        }
        
        // --- simulate read/write error methods
        private void SimulateReadErrors(Dictionary<string,EntityValue> entities) {
            foreach (var readPair in readError) {
                var id      = readPair.Key;
                if (entities.TryGetValue(id, out EntityValue value)) {
                    var payload = readPair.Value;
                    if (payload == "READ-ERROR") {
                        value.SetJson("null");
                        var error = new EntityError(EntityErrorType.ReadError, name, id, "simulated read error");
                        value.SetError(error);
                    } else {
                        value.SetJson(payload);
                    }
                }
            }
        }
        
        private void SimulateWriteErrors(Dictionary<string, EntityValue> entities, Dictionary<string, EntityError> errors) {
            foreach (var writePair in writeError) {
                var id      = writePair.Key;
                if (entities.TryGetValue(id, out EntityValue value)) {
                    var payload = writePair.Value;
                    if (payload == "WRITE-ERROR") {
                        var error = new EntityError(EntityErrorType.WriteError, name, id, "simulated write error");
                        errors.Add(id, error);
                    } else {
                        value.SetJson(payload);
                    }
                }
            }
        }
    }
}