// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Auth;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Sync;

#if UNITY_5_3_OR_NEWER
    using ValueTask = System.Threading.Tasks.Task;
#endif

namespace Friflo.Json.Flow.UserDatabase
{
    internal class AuthCred {
        internal readonly   string          token;
        internal readonly   List<string>    roles;
        
        internal AuthCred (string token, List<string> roles) {
            this.token  = token;
            this.roles  = roles;
        }
    }
    
    internal class ClientCredentials {
        internal readonly   string          token;
        internal            IEventTarget    target;
        internal readonly   Authorizer      authorizer;
        
        internal ClientCredentials (string token, IEventTarget target, Authorizer authorizer) {
            this.token          = token;
            this.target         = target;
            this.authorizer    = authorizer;
        }
    }
    
    public class UserAuthenticator : Authenticator
    {
        private   readonly  UserStore                                               userStore;
        private   readonly  ConcurrentDictionary<IEventTarget, ClientCredentials>   credByTarget;
        private   readonly  ConcurrentDictionary<string,       ClientCredentials>   credByClient;
        private   readonly  Authorizer                                              unknown;
        
        
        public UserAuthenticator (UserStore userStore, Authorizer unknown) {
            this.userStore  = userStore;
            credByTarget    = new ConcurrentDictionary<IEventTarget, ClientCredentials>();
            credByClient    = new ConcurrentDictionary<string,       ClientCredentials>();
            this.unknown    = unknown ?? throw new NullReferenceException(nameof(unknown));
        }
        
        public override async ValueTask Authenticate(SyncRequest syncRequest, MessageContext messageContext)
        {
            var clientId = syncRequest.clientId;
            if (clientId == null) {
                messageContext.authState.SetFailed("user authentication requires clientId", unknown);
                return;
            }
            var eventTarget = messageContext.eventTarget;
            // already authorized?
            if (eventTarget != null && credByTarget.TryGetValue(eventTarget, out ClientCredentials credential)) {
                messageContext.authState.SetSuccess(credential.authorizer);
                return;
            }
            var token = syncRequest.token;
            if (token == null) {
                messageContext.authState.SetFailed("user authentication requires token", unknown);
                return;
            }
            if (!credByClient.TryGetValue(clientId, out credential)) {
                var result = await userStore.ValidateToken(clientId, token);
                
                if (result.isValid && result.roles != null) {
                    var authCred = new AuthCred(token, result.roles);
                    var authorizer  = GetAuthorizer(authCred.roles);
                    credential      = new ClientCredentials (authCred.token, eventTarget, authorizer);
                    credByClient.TryAdd(clientId,    credential);
                    credByTarget.TryAdd(eventTarget, credential);
                }
            }
            if (credential == null || token != credential.token) {
                messageContext.authState.SetFailed("invalid user token", unknown);
                return;
            }
            // Update target if changed for early out when already authorized.
            if (credential.target != eventTarget) {
                if (credential.target != null) {
                    credByTarget.TryRemove(credential.target, out _);
                    credential.target = eventTarget;
                    credByTarget.TryAdd(eventTarget, credential);
                }
            }
            messageContext.authState.SetSuccess(credential.authorizer);
        }
        
        protected virtual Authorizer GetAuthorizer(ICollection<string> roles) {
            if (roles == null || roles.Count == 0) {
                return unknown;
            }
            var authorizers = new List<Authorizer>(roles.Count);
            foreach (var role in roles) {
                if (!PredefinedRoles.Roles.TryGetValue(role, out Authorizer authorizer)) {
                    throw new InvalidOperationException($"unknown authorization role: {role}");
                }
                authorizers.Add(authorizer);
            }
            var any = new AuthorizeAny(authorizers);
            return any;
        }
    }
}