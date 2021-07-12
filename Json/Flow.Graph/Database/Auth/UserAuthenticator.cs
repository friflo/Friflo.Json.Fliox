// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Auth
{
    public class UserStore : EntityStore
    {
        public readonly EntitySet<UserProfile> users;
        
        public UserStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            users = new EntitySet<UserProfile>(this);
        }
    }
    
    public class UserProfile : Entity {
        public string       name;
        public string       passwordHash;
        public string       token;
        public List<string> roles;
    }
    
    internal class AuthCred {
        internal readonly   string          token;
        internal readonly   List<string>    roles;
        
        internal AuthCred (string token, List<string> roles) {
            this.token  = token;
            this.roles  = roles;
        }
    }
    
    internal class ClientCredentials {
        internal readonly   string              token;
        internal            IEventTarget        target;
        internal readonly   List<Authorizer>    authorizers;
        
        internal ClientCredentials (string token, IEventTarget target, List<Authorizer> authorizers) {
            this.token          = token;
            this.target         = target;
            this.authorizers    = authorizers;
        }
    }
    
    public class UserAuthenticator : Authenticator
    {
        private   readonly  UserStore                                               userStore;
        private   readonly  ConcurrentDictionary<IEventTarget, ClientCredentials>   credByTarget;
        private   readonly  ConcurrentDictionary<string,       ClientCredentials>   credByClient;
        private   readonly  Authorizer                                              anonymousAuthorizer;
        private   readonly  ConcurrentDictionary<string, Authorizer>                predefinedRoles;
        
        public UserAuthenticator (UserStore userStore, Authorizer anonymousAuthorizer) {
            this.userStore              = userStore;
            credByTarget                = new ConcurrentDictionary<IEventTarget, ClientCredentials>();
            credByClient                = new ConcurrentDictionary<string,       ClientCredentials>();
            this.anonymousAuthorizer    = anonymousAuthorizer;
            predefinedRoles               = new ConcurrentDictionary<string, Authorizer>();
            
            predefinedRoles.TryAdd("authorizeAll",      new AuthorizeAll());
            predefinedRoles.TryAdd("authorizeNone",     new AuthorizeNone());
            predefinedRoles.TryAdd("authorizeReadOnly", new AuthorizeReadOnly());
        }
        
        public override async ValueTask Authenticate(SyncRequest syncRequest, MessageContext messageContext)
        {
            var clientId = syncRequest.clientId;
            if (clientId == null) {
                messageContext.authState.SetFailed("user authorization requires: clientId", anonymousAuthorizer);
                return;
            }
            var eventTarget = messageContext.eventTarget;
            // already authorized?
            if (eventTarget != null && credByTarget.TryGetValue(eventTarget, out ClientCredentials credential)) {
                messageContext.authState.SetSuccess(credential.authorizers);
                return;
            }
            var token = syncRequest.token;
            if (token == null) {
                messageContext.authState.SetFailed("user authorization requires: token", anonymousAuthorizer);
                return;
            }
            if (!credByClient.TryGetValue(clientId, out credential)) {
                var authCred    = await GetClientCred(clientId);
                if (authCred != null) {
                    var authorizer  = GetAuthorizers(authCred.roles);
                    credential      = new ClientCredentials (authCred.token, eventTarget, authorizer);
                    credByClient.TryAdd(clientId, credential);
                }
            }
            if (credential == null || token != credential.token) {
                messageContext.authState.SetFailed($"user not authorized. Invalid token. clientId: '{clientId}'", anonymousAuthorizer);
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
            messageContext.authState.SetSuccess(credential.authorizers);
        }
        
        protected virtual List<Authorizer> GetAuthorizers(ICollection<string> roles) {
            var authorizers = new List<Authorizer>();
            foreach (var role in roles) {
                if (!predefinedRoles.TryGetValue(role, out Authorizer authorizer)) {
                    throw new InvalidOperationException($"unknown authorization role: {role}");
                }
                authorizers.Add(authorizer);
            }
            if (authorizers.Count == 0) {
                authorizers.Add(anonymousAuthorizer);
            }
            return authorizers;
        }
        
        private async Task<AuthCred> GetClientCred(string clientId) {
            var readUser = userStore.users.Read();
            var findUserProfile = readUser.Find(clientId);
            await userStore.Sync();
            
            UserProfile userProfile = findUserProfile.Result;
            if (userProfile == null)
                return null;
            var cred = new AuthCred(userProfile.token, userProfile.roles);
            return cred;
        }
    }
}