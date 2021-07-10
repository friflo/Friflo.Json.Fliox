// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
    // todo untested
    public class ClientAuthentication
    {
        readonly ConcurrentDictionary<IEventTarget, ClientCredentials>  credByTarget    = new ConcurrentDictionary<IEventTarget, ClientCredentials>();
        readonly ConcurrentDictionary<string,       ClientCredentials>  credByClient    = new ConcurrentDictionary<string,       ClientCredentials>();

        public virtual Task<SyncResponse> Authorize(SyncRequest syncRequest, MessageContext messageContext) {
            var clientId = syncRequest.clientId;
            if (clientId == null) {
                return Error("authorization requires: clientId");
            }
            var eventTarget = messageContext.eventTarget;
            // already authorized?
            if (credByTarget.ContainsKey(eventTarget)) {
                return null;
            }
            var token = syncRequest.token;
            if (token == null) {
                return Error("authorization requires: token");
            }
            if (!credByClient.TryGetValue(clientId, out var credential)) {
                return Error($"client not authorized. Invalid token. clientId: '{clientId}'");
            }
            if (token != credential.token) {
                return Error($"client not authorized. Invalid token. clientId: '{clientId}'");
            }
            // Update target if changed for early out when already authorized.
            if (credential.target != eventTarget) {
                if (credential.target != null) {
                    credByTarget.Remove(credential.target, out _);
                    credential.target = eventTarget;
                    credByTarget.TryAdd(eventTarget, credential);
                }
            }
            return null;
        }
        
        private static Task<SyncResponse> Error (string message) {
            var response = new SyncResponse{ error = new ErrorResponse{ message = message }};
            return Task.FromResult(response);
        }
    }
    
    internal class ClientCredentials
    {
        internal string         token;
        internal IEventTarget   target;
    }
}