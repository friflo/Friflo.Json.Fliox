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

namespace Friflo.Json.Flow.UserAuth
{
    internal class AuthCred {
        internal readonly   string          token;
        
        internal AuthCred (string token) {
            this.token  = token;
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
    
    public interface IUserAuth {
        Task<AuthenticateUserResult> AuthenticateUser(AuthenticateUser value);
    }
    
    /// <summary>
    /// Performs user authentication by validating the "clientId" and the "token" assigned to an <see cref="Graph.EntityStore"/>
    /// <br></br>
    /// If authentication succeed it set the <see cref="AuthState.Authorizer"/> derived from the roles assigned to the user.
    /// If authentication fails the given default <see cref="Authorizer"/> is used for the user.
    /// </summary>
    public class UserAuthenticator : Authenticator
    {
        private   readonly  UserStore                                               userStore;
        private   readonly  IUserAuth                                               userAuth;
        private   readonly  ConcurrentDictionary<IEventTarget, ClientCredentials>   credByTarget;
        private   readonly  ConcurrentDictionary<string,       ClientCredentials>   credByClient;
        private   readonly  Authorizer                                              unknown;
        private   readonly  ConcurrentDictionary<string,       Authorizer>          authorizerByRole;

        public UserAuthenticator (UserStore userStore, IUserAuth userAuth, Authorizer unknown = null) {
            this.userStore      = userStore;
            this.userAuth       = userAuth;
            credByTarget        = new ConcurrentDictionary<IEventTarget, ClientCredentials>();
            credByClient        = new ConcurrentDictionary<string,       ClientCredentials>();
            this.unknown        = unknown ?? new AuthorizeDeny();
            authorizerByRole    = new ConcurrentDictionary<string,       Authorizer>();
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
                var command = new AuthenticateUser { clientId = clientId, token = token };
                var result  = await userAuth.AuthenticateUser(command);
                
                if (result.isValid) {
                    var authCred    = new AuthCred(token);
                    var authorizer  = await GetAuthorizer(clientId);
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
        
        protected virtual async Task<Authorizer> GetAuthorizer(string clientId) {
            var readPermission = userStore.permissions.Read().Find(clientId);
            await userStore.Sync();
            UserPermission permission = readPermission.Result;
            var roles = permission.roles;
            if (roles == null || roles.Count == 0) {
                return unknown;
            }
            await AddNewRoles(roles);
            var authorizers = new List<Authorizer>(roles.Count);
            foreach (var role in roles) {
                // existence is checked already in AddNewRoles()
                authorizerByRole.TryGetValue(role, out Authorizer authorizer);
                authorizers.Add(authorizer);
            }
            if (authorizers.Count == 1)
                return authorizers[0];
            var any = new AuthorizeAny(authorizers);
            return any;
        }
        
        private async Task AddNewRoles(List<string> roles) {
            var newRoles = new List<string>();
            foreach (var role in roles) {
                if (!authorizerByRole.TryGetValue(role, out _)) {
                    newRoles.Add(role);
                }
            }
            if (newRoles.Count == 0)
                return;
            var readRoles = userStore.roles.Read().FindRange(newRoles);
            await userStore.Sync();
            foreach (var newRolePair in readRoles.Results) {
                var role        = newRolePair.Key;
                Role newRole    = newRolePair.Value;
                if (newRole == null)
                    throw new InvalidOperationException($"authorization role not found: '{role}'");
                var authorizers = new List<Authorizer>(newRole.rights.Count);
                foreach (var right in newRole.rights) {
                    Authorizer authorizer;
                    if (right is RightPredicates predicates) {
                        authorizer = GetPredicatesAuthorizer(predicates);
                    } else {
                        authorizer = right.ToAuthorizer();
                    }
                    authorizers.Add(authorizer);
                }
                if (authorizers.Count == 1) {
                    authorizerByRole.TryAdd(role, authorizers[0]);
                } else {
                    var any = new AuthorizeAny(authorizers);
                    authorizerByRole.TryAdd(role, any);
                }
            }
        }
        
        private Authorizer GetPredicatesAuthorizer(RightPredicates right) {
            var authorizers = new List<Authorizer>(right.predicates.Count);
            foreach (var predicateName in right.predicates) {
                if (!registeredPredicates.TryGetValue(predicateName, out var predicate)) {
                    throw new InvalidOperationException($"unknown authorization predicate: {predicateName}");
                }
                authorizers.Add(predicate);
            }
            if (authorizers.Count == 1) {
                return authorizers[0];
            }
            return new AuthorizeAny(authorizers);
        }
    }
}