// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedMember.Local
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public sealed class SignalingService : DatabaseService
    {
        private readonly    Dictionary<ShortString, ConnectRequest> connectMap;

        public  static      DatabaseSchema      Schema { get; } = new DatabaseSchema(typeof(Signaling));

        public SignalingService() {
            connectMap  = new Dictionary<ShortString, ConnectRequest>(ShortString.Equality);
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
            var clientId    = command.ClientId;
            var connectRequest  = new ConnectRequest(clientId);
            bool added;
            lock (connectMap) {
                added = connectMap.TryAdd(clientId, connectRequest);
            }
            if (!added) {
                return command.Error<ConnectClientResult>($"WebRTC connect already pending. host: '{hostId}' client: {clientId}");
            }
            var offer       = new Offer { sdp = value.offerSDP, client = clientId };
            var offerMsg    = signaling.SendMessage(nameof(Offer), offer);
            offerMsg.EventTargetClient(webRtcHost.client);
            await signaling.TrySyncTasks().ConfigureAwait(false);
            
            if (!offerMsg.Success) {
                lock (connectMap) {
                    connectMap.Remove(clientId);
                }
                return command.Error<ConnectClientResult>($"WebRTC connect failed. host: '{hostId}' client: {clientId} error: {offerMsg.Error.Message}");
            }
            var answerSDP = await connectRequest.response.Task.ConfigureAwait(false);
            command.Logger.Log(HubLog.Info, $"WebRTC connect successful. host: '{hostId}' client: {clientId}");
            
            return new ConnectClientResult { answerSDP = answerSDP.sdp };
        }
        
        private void Answer (Param<Answer> param, MessageContext command)
        {
            var logger = command.Logger;
            if (!param.GetValidate(out var answerSDP, out string error)) {
                logger.Log(HubLog.Error, $"invalid answer SDP from '{command.ClientId}' error: {error}");
                return;
            }
            bool found;
            ConnectRequest connectRequest;
            lock (connectMap) {
                found = connectMap.Remove(answerSDP.client, out connectRequest);
            }
            if (!found) {
                logger.Log(HubLog.Error, $"no target for answer SDP. target: {answerSDP.client}");
                return;
            }
            connectRequest.response.SetResult(answerSDP);
        }
    }
    
    public readonly struct ConnectRequest
    {
        private  readonly   ShortString                     clientId;
        public   readonly   TaskCompletionSource<Answer>    response;

        public   override   string                          ToString() => $"client: {clientId}";

        public ConnectRequest(in ShortString clientId) {
            this.clientId   = clientId;
            response        = new TaskCompletionSource<Answer>();
        }
    }
}