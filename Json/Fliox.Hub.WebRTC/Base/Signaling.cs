// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnassignedReadonlyField
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public class Signaling : FlioxClient
    {
        public CommandTask<RegisterHostResult>  RegisterHost (RegisterHost  param) => send.Command<RegisterHost,  RegisterHostResult>   (param);
        public CommandTask<ConnectClientResult> ConnectClient(ConnectClient param) => send.Command<ConnectClient, ConnectClientResult>  (param);
        
        // --- containers
        public  readonly    EntitySet <string, WebRtcHost>         hosts;

        public Signaling (FlioxHub hub, string dbName = null) : base(hub, dbName) { }
        
        public async Task Register(string name) {
            SubscribeMessage<IceCandidate>("IceCandidate", (message, context) => {
            });
            RegisterHost(new RegisterHost { name = name });
            await SyncTasks().ConfigureAwait(false);
        }
    }
}
