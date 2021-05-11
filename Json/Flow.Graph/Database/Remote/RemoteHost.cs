// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Remote
{
    public class RemoteHostDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  local;
        private readonly    ObjectMapper    jsonMapper;


        public RemoteHostDatabase(EntityDatabase local) {
            jsonMapper = new ObjectMapper {WriteNullMembers = false};
            this.local = local;
        }
        
        public override void Dispose() {
            base.Dispose();
            jsonMapper.Dispose();
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            EntityContainer localContainer = local.CreateContainer(name, local);
            RemoteHostContainer container = new RemoteHostContainer(name, this, localContainer);
            return container;
        }

        protected async Task<string> ExecuteSyncJson(string jsonSyncRequest) {
            var syncRequest = jsonMapper.Read<SyncRequest>(jsonSyncRequest);
            SyncResponse syncResponse = await Execute(syncRequest);
            jsonMapper.Pretty = true;
            var jsonResponse = jsonMapper.Write(syncResponse);
            return jsonResponse;
        }
    }
    
    public class RemoteHostContainer : EntityContainer
    {
        private readonly    EntityContainer local;
        
        public  override    bool            Pretty       => local.Pretty;
        public  override    SyncContext     SyncContext  => local.SyncContext;

        public RemoteHostContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }


        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command) {
            return await local.CreateEntities(command);
        }

        public override async Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command) {
            return await local.UpdateEntities(command);
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command) {
            return await local.ReadEntities(command);
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command) {
            return await local.QueryEntities(command);
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command) {
            return await local.DeleteEntities(command);
        }
    }
}
