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
        
        public static void Dispose() {
            var s = _singleton;
            if (s == null)
                return;
            _singleton = null;
            s.Dispose();
        }
        
        private static TypeStore Get() {
            if (_singleton == null) {
                _singleton = new TypeStore();
                _singleton.GetTypeMapper(typeof(DatabaseRequest));
                _singleton.GetTypeMapper(typeof(DatabaseResponse));
                _singleton.GetTypeMapper(typeof(ResponseError));
            }
            return _singleton;
        }

        /// <summary> Returned <see cref="ObjectMapper"/> dont throw Read() exceptions. To handle errors its
        /// <see cref="ObjectMapper.reader"/> -> <see cref="ObjectReader.Error"/> need to be checked. </summary>
        internal static ObjectMapper CreateObjectMapper() {
            var mapper = new ObjectMapper(Get(), new NoThrowHandler());
            return mapper;
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

        protected abstract Task<JsonResponse> ExecuteRequestJson(string jsonRequest, SyncContext syncContext);
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            var response = await ExecuteRequest(syncRequest, syncContext).ConfigureAwait(false);
            if (response is SyncResponse syncResponse)
                return syncResponse;
            var error = (ResponseError)response;
            return new SyncResponse {error = error};
        }
        
        private async Task<DatabaseResponse> ExecuteRequest(DatabaseRequest request, SyncContext syncContext) {
            using (var pooledMapper = syncContext.pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                mapper.Pretty = true;
                mapper.WriteNullMembers = false;
                var jsonRequest = mapper.Write(request);
                var result = await ExecuteRequestJson(jsonRequest, syncContext).ConfigureAwait(false);
                ObjectReader reader = mapper.reader;
                if (result.statusType == RequestStatusType.Ok) {
                    var response = reader.Read<DatabaseResponse>(result.body);
                    if (reader.Error.ErrSet)
                        return new ResponseError{message = reader.Error.msg.ToString()};
                    // At this point the returned result.body is valid JSON.
                    // => All entities of a SyncResponse.results have either a valid JSON value or an error. 
                    return response;
                }
                var responseError = reader.Read<ResponseError>(result.body);
                if (reader.Error.ErrSet)
                    return new ResponseError{message = reader.Error.msg.ToString()};
                return responseError;
            }
        }
    }
    
    public class RemoteClientContainer : EntityContainer
    {
        public RemoteClientContainer(string name, EntityDatabase database)
            : base(name, database) {
        }

        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }

        public override Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }

        public override Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }
        
        public override Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }
    }
}
