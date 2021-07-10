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

        public virtual Task Authenticate(SyncRequest syncRequest, MessageContext messageContext) {
            var clientId = syncRequest.clientId;
            if (clientId == null) {
                messageContext.authenticationError = "authorization requires: clientId";
                return Task.CompletedTask;
            }
            var eventTarget = messageContext.eventTarget;
            // already authorized?
            if (eventTarget != null && credByTarget.ContainsKey(eventTarget)) {
                return Task.CompletedTask;
            }
            var token = syncRequest.token;
            if (token == null) {
                messageContext.authenticationError = "authorization requires: token";
                return Task.CompletedTask;
            }
            if (!credByClient.TryGetValue(clientId, out var credential)) {
                messageContext.authenticationError = $"client not authorized. Invalid token. clientId: '{clientId}'";
                return Task.CompletedTask;
            }
            if (token != credential.token) {
                messageContext.authenticationError = $"client not authorized. Invalid token. clientId: '{clientId}'";
                return Task.CompletedTask;
            }
            // Update target if changed for early out when already authorized.
            if (credential.target != eventTarget) {
                if (credential.target != null) {
                    credByTarget.Remove(credential.target, out _);
                    credential.target = eventTarget;
                    credByTarget.TryAdd(eventTarget, credential);
                }
            }
            return Task.CompletedTask;
        }
    }
    
    internal class ClientCredentials
    {
        internal string         token;
        internal IEventTarget   target;
    }
}