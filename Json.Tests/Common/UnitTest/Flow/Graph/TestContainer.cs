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
            EntityContainer localContainer = local.CreateContainer(name, local);
            TestContainer container = new TestContainer(name, this, localContainer);
            return container;
        }
    }
    
    public class TestContainer : EntityContainer
    {
        private readonly    EntityContainer local;
        
        public  override    bool            Pretty       => local.Pretty;
        public  override    SyncContext     SyncContext  => local.SyncContext;

        public TestContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }

        public override async Task<CreateEntitiesResult>    CreateEntities  (CreateEntities task) {
            return await local.CreateEntities(task);
        }

        public override async Task<UpdateEntitiesResult>    UpdateEntities  (UpdateEntities task) {
            return await local.UpdateEntities(task);
        }

        public override async Task<ReadEntitiesResult>      ReadEntities    (ReadEntities task) {
            return await local.ReadEntities(task);
        }
        
        public override async Task<QueryEntitiesResult>     QueryEntities   (QueryEntities task) {
            return await local.QueryEntities(task);
        }
        
        public override async Task<DeleteEntitiesResult>    DeleteEntities  (DeleteEntities task) {
            return await local.DeleteEntities(task);
        }
    }
}