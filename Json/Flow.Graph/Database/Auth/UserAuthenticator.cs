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
        
        private   static readonly  ConcurrentDictionary<string, Authorizer>         PredefinedRoles;
        
        public UserAuthenticator (UserStore userStore, Authorizer unknown) {
            this.userStore  = userStore;
            credByTarget    = new ConcurrentDictionary<IEventTarget, ClientCredentials>();
            credByClient    = new ConcurrentDictionary<string,       ClientCredentials>();
            this.unknown    = unknown ?? throw new NullReferenceException(nameof(unknown));
        }
        
        static UserAuthenticator() {
            PredefinedRoles = new ConcurrentDictionary<string, Authorizer>();
            //
            PredefinedRoles.TryAdd("allow",             new AuthorizeAllow());
            PredefinedRoles.TryAdd("deny",              new AuthorizeDeny());
            PredefinedRoles.TryAdd("readOnly",          new AuthorizeReadOnly());
            //
            PredefinedRoles.TryAdd("read",              new AuthorizeTaskType(TaskType.read));
            PredefinedRoles.TryAdd("query",             new AuthorizeTaskType(TaskType.query));
            PredefinedRoles.TryAdd("create",            new AuthorizeTaskType(TaskType.create));
            PredefinedRoles.TryAdd("update",            new AuthorizeTaskType(TaskType.update));
            PredefinedRoles.TryAdd("patch",             new AuthorizeTaskType(TaskType.patch));
            PredefinedRoles.TryAdd("delete",            new AuthorizeTaskType(TaskType.delete));
            PredefinedRoles.TryAdd("subscribeChanges",  new AuthorizeTaskType(TaskType.subscribeChanges));
            PredefinedRoles.TryAdd("subscribeMessage",  new AuthorizeTaskType(TaskType.subscribeMessage));
        }
        
        public override async ValueTask Authenticate(SyncRequest syncRequest, MessageContext messageContext)
        {
            var clientId = syncRequest.clientId;
            if (clientId == null) {
                messageContext.authState.SetFailed("user authorization requires clientId", unknown);
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
                messageContext.authState.SetFailed("user authorization requires token", unknown);
                return;
            }
            if (!credByClient.TryGetValue(clientId, out credential)) {
                var authCred    = await GetClientCred(clientId);
                if (authCred != null) {
                    var authorizer  = GetAuthorizer(authCred.roles);
                    credential      = new ClientCredentials (authCred.token, eventTarget, authorizer);
                    credByClient.TryAdd(clientId, credential);
                }
            }
            if (credential == null || token != credential.token) {
                messageContext.authState.SetFailed($"user not authorized. Invalid token. clientId: '{clientId}', token: '{token}'", unknown);
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
            var authorizers = new List<Authorizer>();
            foreach (var role in roles) {
                if (!PredefinedRoles.TryGetValue(role, out Authorizer authorizer)) {
                    throw new InvalidOperationException($"unknown authorization role: {role}");
                }
                authorizers.Add(authorizer);
            }
            if (authorizers.Count == 0) {
                return unknown;
            }
            var any = new AuthorizeAny(authorizers);
            return any;
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