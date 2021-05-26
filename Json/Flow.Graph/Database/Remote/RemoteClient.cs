// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
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
                _singleton.GetTypeMapper(typeof(SyncErrorResponse));
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

        protected abstract Task<SyncJsonResult> ExecuteSyncJson(string jsonSyncRequest, SyncContext syncContext);

        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            using (var pooledMapper = syncContext.pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                mapper.Pretty = true;
                mapper.WriteNullMembers = false;
                var jsonRequest = mapper.Write(syncRequest);
                var result = await ExecuteSyncJson(jsonRequest, syncContext).ConfigureAwait(false);
                if (result.success) {
                    var response = mapper.Read<SyncResponse>(result.body);
                    return response;
                }
                var errorResponse = mapper.Read<SyncErrorResponse>(result.body);
                return new SyncResponse{error=errorResponse.error};
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
