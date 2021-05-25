// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Remote
{
    public class RemoteHostDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  local;

        public RemoteHostDatabase(EntityDatabase local) {
            this.local = local;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            EntityContainer localContainer = local.CreateContainer(name, local);
            RemoteHostContainer container = new RemoteHostContainer(name, this, localContainer);
            return container;
        }

        public async Task<string> ExecuteSyncJson(string jsonSyncRequest, SyncContext syncContext) {
            using (var jsonMapper = syncContext.pools.objectMapper.Get()) {
                var mapper = jsonMapper.value;
                var syncRequest = mapper.Read<SyncRequest>(jsonSyncRequest);
                SyncResponse syncResponse = await ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
                mapper.WriteNullMembers = false;
                mapper.Pretty = true;
                var jsonResponse = mapper.Write(syncResponse);
                return jsonResponse;
            }
        }
    }
    
    public class RemoteHostContainer : EntityContainer
    {
        private readonly    EntityContainer local;
        
        public  override    bool            Pretty       => local.Pretty;

        public RemoteHostContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }


        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            return await local.CreateEntities(command, syncContext).ConfigureAwait(false);
        }

        public override async Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command, SyncContext syncContext) {
            return await local.UpdateEntities(command, syncContext).ConfigureAwait(false);
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
            return await local.ReadEntities(command, syncContext).ConfigureAwait(false);
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            return await local.QueryEntities(command, syncContext).ConfigureAwait(false);
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            return await local.DeleteEntities(command, syncContext).ConfigureAwait(false);
        }
    }
}
