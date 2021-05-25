// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Remote
{
    /// Singleton are typically a bad practice, but its okay in this case as <see cref="TypeStore"/> behaves like an
    /// immutable object because the mapped types <see cref="SyncRequest"/> and <see cref="SyncResponse"/> are
    /// a fixed set of types. 
    public static class SyncTypeStore
    {
        private static TypeStore _singleton;

        public static void Init() {
            Get();
        }
        
        internal static TypeStore Get() {
            if (_singleton == null) {
                _singleton = new TypeStore();
                _singleton.GetTypeMapper(typeof(SyncRequest));
                _singleton.GetTypeMapper(typeof(SyncResponse));
            }
            return _singleton;
        }
    }
    
    public abstract class RemoteClientDatabase : EntityDatabase
    {
        
        public RemoteClientDatabase() {
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            RemoteClientContainer container = new RemoteClientContainer(name, this);
            return container;
        }

        protected abstract Task<string> ExecuteSyncJson(string jsonSynRequest);

        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            using (var pooledMapper = syncContext.pools.objectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.value;
                mapper.Pretty = true;
                mapper.WriteNullMembers = false;
                var jsonRequest = mapper.Write(syncRequest);
                var jsonResponse = await ExecuteSyncJson(jsonRequest).ConfigureAwait(false);
                var response = mapper.Read<SyncResponse>(jsonResponse);
                return response;
            }
        }
    }
    
    public class RemoteClientContainer : EntityContainer
    {
        public RemoteClientContainer(string name, EntityDatabase database)
            : base(name, database) {
        }

        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }

        public override Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }

        public override Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }
        
        public override Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("ClientContainer does not execute CRUD commands");
        }
    }
}
