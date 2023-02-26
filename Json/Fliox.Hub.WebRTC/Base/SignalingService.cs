// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedMember.Local
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public class SignalingService : DatabaseService
    {
        private readonly    Dictionary<ShortString, ConnectRequest> connectMap;

        public  static      DatabaseSchema      Schema { get; } = new DatabaseSchema(typeof(Signaling));

        public SignalingService() {
            connectMap  = new Dictionary<ShortString, ConnectRequest>(ShortString.Equality);
            AddMessageHandlers(this, null);
        }
        
        private async Task<RegisterHostResult> RegisterHost (Param<RegisterHost> param, MessageContext command)
        {
            if (!param.GetValidate(out var value, out string error)) {
                return command.Error<RegisterHostResult>(error);
            }
            var signaling   = new Signaling(command.Hub, command.Database.name)  { UserInfo = command.UserInfo };
            var webRtcHost  = new WebRtcHost { id = value.name, client = command.ClientId.AsString() };
            signaling.hosts.Upsert(webRtcHost);
            await signaling.SyncTasks().ConfigureAwait(false);

            return new RegisterHostResult();
        }
        
        private async Task<ConnectClientResult> ConnectClient (Param<ConnectClient> param, MessageContext command)
        {
            if (!param.GetValidate(out var value, out string error)) {
                return command.Error<ConnectClientResult>(error);
            }
            // --- find WebRTC Host in database
            var hostId      = value.name;
            var signaling   = new Signaling(command.Hub, command.Database.name)  { UserInfo = command.UserInfo };
            var findHost    = signaling.hosts.Read().Find(hostId);
            await signaling.SyncTasks().ConfigureAwait(false);
            
            var webRtcHost = findHost.Result;
            if (webRtcHost == null) {
                return command.Error<ConnectClientResult>($"host not found. name: {hostId}");
            }
            // --- send offer SDP to WebRTC host 
            var clientId    = command.ClientId;
            var offer       = new Offer { sdp = value.offerSDP, client = clientId };
            var offerMsg    = signaling.SendMessage(nameof(Offer), offer);
            offerMsg.EventTargets.AddClient(webRtcHost.client);
            await signaling.SyncTasks().ConfigureAwait(false);
            
            var connectRequest  = new ConnectRequest(clientId);
            connectMap.Add(clientId, connectRequest);
            var answerSDP       = await connectRequest.response.Task.ConfigureAwait(false);

            return new ConnectClientResult { answerSDP = answerSDP.sdp };
        }
        
        private void AnswerSDP (Param<Answer> param, MessageContext command)
        {
            if (!param.GetValidate(out var answerSDP, out string error)) {
                return;
            }
            if (!connectMap.TryGetValue(answerSDP.client, out var connectRequest)) {
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