// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedMember.Local
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public sealed class SignalingService : DatabaseService
    {
        private const       string              LogName = "Signaling";

        public  static      DatabaseSchema      Schema { get; } = new DatabaseSchema(typeof(Signaling));

        public SignalingService() {
            AddMessageHandlers(this, null);
        }
        
        private static async Task<AddHostResult> AddHost (Param<AddHost> param, MessageContext command)
        {
            if (!param.GetValidate(out var value, out string error)) {
                return command.Error<AddHostResult>(error);
            }
            var signaling   = new Signaling(command.Hub, command.Database.name)  { UserInfo = command.UserInfo };
            var webRtcHost  = new WebRtcHost { id = value.hostId, client = command.ClientId.AsString() };
            signaling.hosts.Upsert(webRtcHost);
            await signaling.SyncTasks().ConfigureAwait(false);
            
            command.Logger.Log(HubLog.Info, $"{LogName}: host added. host: '{value.hostId}' client: {command.ClientId}");
            return new AddHostResult();
        }
        
        private async Task<ConnectClientResult> ConnectClient (Param<ConnectClient> param, MessageContext command)
        {
            if (!param.GetValidate(out var value, out string error)) {
                return command.Error<ConnectClientResult>(error);
            }
            // --- find WebRTC Host in database
            var hostId      = value.hostId;
            var signaling   = new Signaling(command.Hub, command.Database.name)  { UserInfo = command.UserInfo };
            var findHost    = signaling.hosts.Read().Find(hostId);
            await signaling.SyncTasks().ConfigureAwait(false);
            
            var webRtcHost = findHost.Result;
            if (webRtcHost == null) {
                return command.Error<ConnectClientResult>($"WebRTC connect failed. host not found. host: '{hostId}'");
            }
            // --- send offer SDP to WebRTC host 
            var clientId        = command.ClientId;
            var offer           = new Offer { sdp = value.offerSDP, client = clientId };
            var offerMsg        = signaling.SendMessage(nameof(Offer), offer);
            offerMsg.EventTargetClient(webRtcHost.client);
            _ = signaling.TrySyncTasks().ConfigureAwait(false);
            
            return new ConnectClientResult { hostClientId = webRtcHost.client };
        }
    }
}