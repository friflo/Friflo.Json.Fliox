// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs
{
    ///<summary>
    /// Provide same behavior as <see cref="HttpClientHub"/> / <see cref="HttpListenerHost"/> regarding
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

        public LoopbackHub(FlioxHub hub, string hostName = null) : base(hub.database, hub.sharedEnv, hostName) {
            host = hub;
        }

        public override void Dispose() {
            base.Dispose();
            host.Dispose();
        }
        
        public override async Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            var response = await host.ExecuteSync(syncRequest, messageContext);
            return response;
        }
    }
}