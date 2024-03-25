// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedMember.Local
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public sealed class SignalingCommands : IServiceCommands
    {
        private const       string              LogName = "Signaling";

        public  static      DatabaseSchema      Schema { get; } = DatabaseSchema.Create<Signaling>();

        public SignalingCommands() {
        }
        
        [CommandHandler]
        private static async Task<Result<AddHostResult>> AddHost (Param<AddHost> param, MessageContext context)
        {
            if (!param.GetValidate(out var value, out string error)) {
                return Result.ValidationError(error);
            }
            var signaling   = new Signaling(context.Hub, context.Database.name)  { UserInfo = context.UserInfo };
            var webRtcHost  = new WebRtcHost { id = value.hostId, client = context.ClientId.AsString() };
            signaling.hosts.Upsert(webRtcHost);
            await signaling.SyncTasks().ConfigureAwait(false);
            
            context.Logger.Log(HubLog.Info, $"{LogName}: host added. host: '{value.hostId}' client: {context.ClientId}");
            return new AddHostResult();
        }
        
        [CommandHandler]
        private async Task<Result<ConnectClientResult>> ConnectClient (Param<ConnectClient> param, MessageContext context)
        {
            if (!param.GetValidate(out var value, out string error)) {
                return Result.ValidationError(error);
            }
            // --- find WebRTC Host in database
            var hostId      = value.hostId;
            var signaling   = new Signaling(context.Hub, context.Database.name)  { UserInfo = context.UserInfo };
            var findHost    = signaling.hosts.Read().Find(hostId);
            await signaling.SyncTasks().ConfigureAwait(false);
            
            var webRtcHost = findHost.Result;
            if (webRtcHost == null) {
                return Result.Error($"WebRTC connect failed. host not found. host: '{hostId}'");
            }
            // --- send offer SDP to WebRTC host 
            var clientId        = context.ClientId;
            var offer           = new Offer { sdp = value.offerSDP, client = clientId };
            var offerMsg        = signaling.SendMessage(nameof(Offer), offer);
            offerMsg.EventTargetClient(webRtcHost.client);
            _ = signaling.TrySyncTasks().ConfigureAwait(false);
            
            return new ConnectClientResult { hostClientId = webRtcHost.client };
        }
    }
}