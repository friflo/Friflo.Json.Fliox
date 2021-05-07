using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Burst;  // UnityExtension.TryAdd(), ToHashSet()
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
        public  readonly    Dictionary<string, string>  readErrors  = new Dictionary<string, string>();
        public  readonly    HashSet<string>             writeTaskErrors = new HashSet<string>();
        public  readonly    HashSet<string>             queryTaskErrors = new HashSet<string>();
        
        public  override    bool            Pretty       => local.Pretty;
        public  override    SyncContext     SyncContext  => local.SyncContext;

        public TestContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }

        public override async Task<CreateEntitiesResult>    CreateEntities  (CreateEntities command) {
            SimulateWriteErrors(command.entities.Keys.ToHashSet());
            var result = await local.CreateEntities(command);
            return result;
        }

        public override async Task<UpdateEntitiesResult>    UpdateEntities  (UpdateEntities command) {
            SimulateWriteErrors(command.entities.Keys.ToHashSet());
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
            if (queryTaskErrors.Contains(command.filterLinq)) {
                throw new SimulationException("EntityContainer query exception");
            }
            return result;
        }
        
        public override async Task<DeleteEntitiesResult>    DeleteEntities  (DeleteEntities command) {
            SimulateWriteErrors(command.ids);
            return await local.DeleteEntities(command);
        }
        
        // --- simulate read/write error methods
        private void SimulateReadErrors(Dictionary<string,EntityValue> entities) {
            foreach (var readPair in readErrors) {
                var id      = readPair.Key;
                if (entities.TryGetValue(id, out EntityValue value)) {
                    var payload = readPair.Value;
                    switch (payload) {
                        case Simulate.ReadEntityError:
                            value.SetJson("null");
                            var error = new EntityError(EntityErrorType.ReadError, name, id, "simulated read error");
                            value.SetError(error);
                            break;
                        case Simulate.ReadTaskException:
                            throw new SimulationException("EntityContainer read exception");
                        default:
                            value.SetJson(payload); // modify JSON
                            break;
                    }
                }
            }
        }
        
        private void SimulateWriteErrors(HashSet<string> entities) {
            foreach (var id in writeTaskErrors) {
                if (entities.Contains(id)) {
                    throw new SimulationException("EntityContainer write exception");
                }
            }
        }
    }

    public static class Simulate
    {
        public const string ReadEntityError    = "READ-ENTITY-ERROR";
        public const string ReadTaskException  = "READ-TASK-EXCEPTION";
    }    

    public class SimulationException : Exception {
        public SimulationException(string message) : base(message) { }
    }
}
