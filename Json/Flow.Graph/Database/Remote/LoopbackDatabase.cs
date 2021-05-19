// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Remote
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
        private readonly RemoteHostDatabase loopbackHost;
        
        public LoopbackDatabase(EntityDatabase local) {
            loopbackHost = new RemoteHostDatabase(local);
        }

        public override void Dispose() {
            base.Dispose();
            loopbackHost.Dispose();
        }

        protected override async Task<string> ExecuteSyncJson(string jsonSynRequest) {
            var jsonResponse = await loopbackHost.ExecuteSyncJson(jsonSynRequest).ConfigureAwait(false);
            return jsonResponse;
        }
    }
}