// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedMember.Local
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public class SignalingService : DatabaseService
    {
        private readonly    FlioxHub            hub;
        private readonly    WebRtcConfig        config;

        public  static      DatabaseSchema      Schema { get; } = new DatabaseSchema(typeof(Signaling));

        public SignalingService(FlioxHub hub, WebRtcConfig config) {
            this.hub    = hub;
            this.config = config;
            AddMessageHandlers(this, null);
        }
        
        private async Task<RegisterHostResult> RegisterHost (Param<RegisterHost> param, MessageContext command) {
            if (!param.GetValidate(out var value, out string error)) {
                return command.Error<RegisterHostResult>(error);
            }
            var signaling  = new Signaling(command.Hub, command.Database.name)  { UserInfo = command.UserInfo };
            var webRtcHost = new WebRtcHost { id = value.name, client = command.ClientId.AsString() };
            signaling.hosts.Upsert(webRtcHost);
            await signaling.SyncTasks().ConfigureAwait(false);
            
#if !UNITY_5_3_OR_NEWER
            _ = RtcSocketHost.SendReceiveMessages(config, null, hub);
#endif
            return new RegisterHostResult();
        }
        private ConnectClientResult ConnectClient (Param<ConnectClient> param, MessageContext command) {
            if (!param.GetValidate(out var value, out string error)) {
                return command.Error<ConnectClientResult>(error);
            }

#if !UNITY_5_3_OR_NEWER
            _ = RtcSocketHost.SendReceiveMessages(config, null, hub);
#endif
            return new ConnectClientResult();
        }
    }
}