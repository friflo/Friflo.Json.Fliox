// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
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
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            var result = await local.ExecuteSync(syncRequest, syncContext);
            return result;
        }

        public async Task<SyncJsonResult> ExecuteSyncJson(string jsonSyncRequest) {
            var contextPools    = new Pools(Pools.SharedPools);
            var syncContext     = new SyncContext(contextPools);
            try {
                string jsonResponse;
                using (var pooledMapper = syncContext.pools.ObjectMapper.Get()) {
                    ObjectMapper mapper = pooledMapper.instance;
                    ObjectReader reader = mapper.reader;
                    var syncRequest = reader.Read<SyncRequest>(jsonSyncRequest);
                    if (reader.Error.ErrSet)
                        return SyncJsonResult.CreateSyncError(syncContext, reader.Error.msg.ToString(), SyncStatusType.Error);
                    SyncResponse syncResponse = await ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
                    mapper.WriteNullMembers = false;
                    mapper.Pretty = true;
                    jsonResponse = mapper.Write(syncResponse);
                }
                syncContext.pools.AssertNoLeaks();
                return new SyncJsonResult(jsonResponse, SyncStatusType.Ok);
            } catch (Exception e) {
                var errorMsg = SyncError.ErrorFromException(e).ToString();
                return SyncJsonResult.CreateSyncError(syncContext, errorMsg, SyncStatusType.Exception);
            }
        }
    }
    
    public enum SyncStatusType {
        /// maps to HTTP 200 OK
        Ok,         
        /// maps to HTTP 400 Bad Request
        Error,
        /// maps to HTTP 500 Internal Server Error
        Exception
    }
    
    public class SyncJsonResult
    {
        public readonly     string          body;
        public readonly     SyncStatusType  statusType;
        
        public SyncJsonResult(string body, SyncStatusType statusType) {
            this.body       = body;
            this.statusType  = statusType;
        }
        
        public static SyncJsonResult CreateSyncError(SyncContext syncContext, string message, SyncStatusType type) {
            var syncError = new SyncError {message = message};
            using (var pooledMapper = syncContext.pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                var body = mapper.Write(syncError);
                return new SyncJsonResult(body, type);
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
