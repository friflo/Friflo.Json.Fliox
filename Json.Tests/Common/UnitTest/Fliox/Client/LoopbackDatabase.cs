// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    ///<summary>
    /// Provide same behavior as <see cref="HttpClientDatabase"/> / <see cref="HttpHostDatabase"/> regarding
    /// serialization of <see cref="SyncRequest"/> and deserialization of <see cref="SyncResponse"/>.
    /// 
    /// This features allows testing a remote client/host scenario with the following features:
    /// <para>No extra time required to setup an HTTP client and server.</para>
    /// <para>No creation of an extra thread for the HTTP server.</para>
    /// <para>Simplify debugging as only a single thread is running.</para>
    /// </summary>
    public class LoopbackDatabase : RemoteClientDatabase
    {
        private readonly    RemoteHostDatabase  loopbackHost;

        public LoopbackDatabase(EntityDatabase local) {
            loopbackHost = new RemoteHostDatabase(local);
        }

        public override void Dispose() {
            base.Dispose();
            loopbackHost.Dispose();
        }
        
        public override async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            var response = await loopbackHost.ExecuteSync(syncRequest, messageContext);
            return response;
        }
    }
}