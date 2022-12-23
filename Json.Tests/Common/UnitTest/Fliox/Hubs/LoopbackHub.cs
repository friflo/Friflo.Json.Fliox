// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;

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
    public class LoopbackHub : RemoteClientHub
    {
        public readonly    FlioxHub  host;

        public LoopbackHub(FlioxHub hub)
            : base(hub.database, hub.sharedEnv)
        {
            host = hub;
        }

        public override void Dispose() {
            base.Dispose();
            host.Dispose();
        }
        
        public override ExecutionType InitSyncRequest(SyncRequest syncRequest) {
            base.InitSyncRequest(syncRequest);
            return ExecutionType.Async;
        }
        
        public override async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext) {
            using (var pooledMapper = syncContext.ObjectMapper.Get()) {
                var mapper          = pooledMapper.instance;
                var requestJson     = RemoteUtils.CreateProtocolMessage(syncRequest, mapper.writer);
                var requestCopy     = RemoteUtils.ReadSyncRequest (mapper.reader, requestJson, out var _);
                host.InitSyncRequest(requestCopy);
                var syncResponse    = await host.ExecuteRequestAsync(requestCopy, syncContext);
                
                if (syncResponse.error != null) {
                    return syncResponse;
                }
                RemoteHost.SetContainerResults(syncResponse.success);
                var responseJson    = RemoteUtils.CreateProtocolMessage(syncResponse.success, mapper.writer);
                var responseMessage = RemoteUtils.ReadProtocolMessage (responseJson, mapper.reader, out _);
                var responseCopy    = (SyncResponse)responseMessage;
                
                return new ExecuteSyncResult(responseCopy);
            }
        }
    }
}