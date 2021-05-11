// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Database
{
    public abstract class RemoteClientDatabase : EntityDatabase
    {
        private readonly ObjectMapper   jsonMapper;
        
        public RemoteClientDatabase() {
            jsonMapper = new ObjectMapper { Pretty = true, WriteNullMembers = false};
        }
        
        public override void Dispose() {
            base.Dispose();
            jsonMapper.Dispose();
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            RemoteClientContainer container = new RemoteClientContainer(name, this);
            return container;
        }

        protected abstract Task<string> ExecuteJson(string jsonSynRequest);

        public override async Task<SyncResponse> Execute(SyncRequest syncRequest) {
            var jsonRequest = jsonMapper.Write(syncRequest);
            var jsonResponse = await ExecuteJson(jsonRequest);
            var response = jsonMapper.Read<SyncResponse>(jsonResponse);
            return response;
        }
    }
    
    public class RemoteClientContainer : EntityContainer
    {
        public RemoteClientContainer(string name, EntityDatabase database)
            : base(name, database) {
        }

        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }

        public override Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }

        public override Task<ReadEntitiesResult> ReadEntities(ReadEntities command) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }
        
        public override Task<QueryEntitiesResult> QueryEntities(QueryEntities command) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }
    }
}
