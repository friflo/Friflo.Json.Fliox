// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Remote.Tools;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs
{
    ///<summary>
    /// Provide same behavior as <see cref="HttpClientHub"/> / <see cref="WebSocketClientHub"/> regarding
    /// serialization of <see cref="SyncRequest"/> and deserialization of <see cref="SyncResponse"/>.
    /// 
    /// This features allows testing a remote client/host scenario with the following features:
    /// <para>No extra time required to setup an HTTP client and server.</para>
    /// <para>No creation of an extra thread for the HTTP server.</para>
    /// <para>Simplify debugging as only a single thread is running.</para>
    /// </summary>
    public class LoopbackHub : FlioxHub
    {
        public readonly FlioxHub hub;

        public LoopbackHub(FlioxHub hub)
            : base(hub.database, hub.sharedEnv)
        {
            this.hub = hub;
        }

        public override void Dispose() {
            base.Dispose();
            hub.Dispose();
        }
        
        protected override    bool    IsRemoteHub         => true;
        
        public override ExecutionType InitSyncRequest(SyncRequest syncRequest) {
            return ExecutionType.Async;
        }
        
        public override async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext) {
            using (var pooledMapper = syncContext.ObjectMapper.Get()) {
                var mapper          = pooledMapper.instance;
                var requestJson     = RemoteMessageUtils.CreateProtocolMessage(syncRequest, mapper.writer);
                var requestCopy     = RemoteMessageUtils.ReadSyncRequest (mapper.reader, sharedEnv.types.protocol, requestJson, out var _);
                hub.InitSyncRequest(requestCopy);
                var syncResponse    = await hub.ExecuteRequestAsync(requestCopy, syncContext);
                
                if (syncResponse.error != null) {
                    return syncResponse;
                }
                RemoteHostUtils.SetContainerResults(syncResponse.success);
                var responseJson    = RemoteMessageUtils.CreateProtocolMessage(syncResponse.success, mapper.writer);
                var responseMessage = RemoteMessageUtils.ReadProtocolMessage (responseJson, mapper.reader, out _);
                var responseCopy    = (SyncResponse)responseMessage;
                
                return new ExecuteSyncResult(responseCopy);
            }
        }
    }
}