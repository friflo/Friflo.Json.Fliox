// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
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
        public string   name;
        public string   passwordHash;
        public string   token;
        public string   role;
    }
    
    internal class AuthCred {
        internal readonly   string token;
        internal readonly   string role;
        
        internal AuthCred (string token, string role) {
            this.token  = token;
            this.role   = role;
        }
    }
    
    internal class ClientCredentials {
        internal readonly   string         token;
        internal            IEventTarget   target;
        internal readonly   Authorizer     authorizer;
        
        internal ClientCredentials (string token, IEventTarget target, Authorizer authorizer) {
            this.token      = token;
            this.target     = target;
            this.authorizer = authorizer;
        }
    }
    
    public class UserAuthenticator : Authenticator
    {
        private   readonly  UserStore                                               userStore;
        private   readonly  ConcurrentDictionary<IEventTarget, ClientCredentials>   credByTarget;
        private   readonly  ConcurrentDictionary<string,       ClientCredentials>   credByClient;
        private   readonly  Authorizer                                              anonymousAuthorizer;
        private   readonly  ConcurrentDictionary<string, Authorizer>                authorizers;
        
        public UserAuthenticator (UserStore userStore, Authorizer anonymousAuthorizer) {
            this.userStore              = userStore;
            credByTarget                = new ConcurrentDictionary<IEventTarget, ClientCredentials>();
            credByClient                = new ConcurrentDictionary<string,       ClientCredentials>();
            this.anonymousAuthorizer    = anonymousAuthorizer;
            this.authorizers            = new ConcurrentDictionary<string, Authorizer>();
            
            authorizers.TryAdd("authorizeAll",      new AuthorizeAll());
            authorizers.TryAdd("authorizeNone",     new AuthorizeNone());
            authorizers.TryAdd("authorizeReadOnly", new AuthorizeReadOnly());
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
                messageContext.authState.SetSuccess(credential.authorizer);
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
                    var authorizer  = GetAuthorizer(authCred.role);
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
            messageContext.authState.SetSuccess(credential.authorizer);
        }
        
        protected virtual Authorizer GetAuthorizer(string role) {
            if (role != null && authorizers.TryGetValue(role, out Authorizer authorizer)) {
                return authorizer;
            }
            return anonymousAuthorizer;
        }
        
        private async Task<AuthCred> GetClientCred(string clientId) {
            var readUser = userStore.users.Read();
            var findUserProfile = readUser.Find(clientId);
            await userStore.Sync();
            
            UserProfile userProfile = findUserProfile.Result;
            if (userProfile == null)
                return null;
            var cred = new AuthCred(userProfile.token, userProfile.role);
            return cred;
        }
    }
}