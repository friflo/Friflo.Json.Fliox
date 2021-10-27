// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Auth;
using Friflo.Json.Fliox.Hub.Auth.Rights;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.UserAuth
{
    internal class AuthCred {
        internal readonly   string          token;
        
        internal AuthCred (string token) {
            this.token  = token;
        }
    }
    
    public interface IUserAuth {
        Task<AuthenticateUserResult> Authenticate(AuthenticateUser value);
    }
    
    /// <summary>
    /// Performs user authentication by validating the "userId" and the "token" assigned to a <see cref="Client.FlioxClient"/>
    /// <br></br>
    /// If authentication succeed it set the <see cref="AuthState.authorizer"/> derived from the roles assigned to the user.
    /// If authentication fails the given default <see cref="Authorizer"/> is used for the user.
    /// </summary>
    public class UserAuthenticator : Authenticator
    {
        private   readonly  UserStore                                   userStore;
        private   readonly  IUserAuth                                   userAuth;
        private   readonly  Authorizer                                  unknown;
        private   readonly  ConcurrentDictionary<string,  Authorizer>   authorizerByRole;


        public UserAuthenticator (UserStore userStore, IUserAuth userAuth, Authorizer unknown = null)
            : base (unknown)
        {
            this.userStore      = userStore;
            this.userAuth       = userAuth;
            this.unknown        = unknown ?? new AuthorizeDeny();
            authorizerByRole    = new ConcurrentDictionary <string, Authorizer>();
        }
        
        public async Task ValidateRoles() {
            var queryRoles = userStore.roles.QueryAll();
            await userStore.TrySyncTasks().ConfigureAwait(false);
            Dictionary<string, Role> roles = queryRoles.Results;
            foreach (var pair in roles) {
                var role = pair.Value;
                foreach (var right in role.rights) {
                    if (!(right is RightPredicate rightPredicates))
                        break;
                    foreach (var predicateName in rightPredicates.names) {
                        if (!registeredPredicates.ContainsKey(predicateName)) {
                            throw new InvalidOperationException($"unknown authorization predicate: {predicateName}");
                        }
                    }
                }
            }
        }

        private const string InvalidUserToken = "Authentication failed";
        
        public override async Task Authenticate(SyncRequest syncRequest, MessageContext messageContext)
        {
            var userId = syncRequest.userId;
            if (userId.IsNull()) {
                messageContext.AuthenticationFailed(anonymousUser, "user authentication requires 'user' id", unknown);
                return;
            }
            var token = syncRequest.token;
            if (token == null) {
                messageContext.AuthenticationFailed(anonymousUser, "user authentication requires 'token'", unknown);
                return;
            }
            if (users.TryGetValue(userId, out User user)) {
                if (user.token != token) {
                    messageContext.AuthenticationFailed(user, InvalidUserToken, unknown);
                    return;
                }
                messageContext.AuthenticationSucceed(user, user.authorizer);
                return;
            }
            var command = new AuthenticateUser { userId = userId, token = token };
            var result  = await userAuth.Authenticate(command).ConfigureAwait(false);
            
            if (result.isValid) {
                var authCred    = new AuthCred(token);
                var authorizer  = await GetAuthorizer(userId).ConfigureAwait(false);
                user        = new User (userId, authCred.token, authorizer);
                users.TryAdd(userId, user);
            }
            
            if (user == null || token != user.token) {
                messageContext.AuthenticationFailed(anonymousUser, InvalidUserToken, unknown);
                return;
            }
            messageContext.AuthenticationSucceed(user, user.authorizer);
        }
        
        public override ClientIdValidation ValidateClientId(ClientController clientController, MessageContext messageContext) {
            ref var clientId = ref messageContext.clientId; 
            if (clientId.IsNull()) {
                return ClientIdValidation.IsNull;
            }
            if (!messageContext.Authenticated) {
                return ClientIdValidation.Invalid;
            }
            var user        = messageContext.User;
            var userClients = user.clients; 
            if (userClients.Contains(clientId)) {
                return ClientIdValidation.Valid;
            }
            // Is clientId already used by another user?
            if (clientController.Clients.ContainsKey(clientId)) {
                return ClientIdValidation.Invalid;
            }
            if (clientController.UseClientIdFor(user, clientId)) {
                userClients.Add(clientId);
                return ClientIdValidation.Valid;
            }
            return ClientIdValidation.Invalid;
        }
        
        public override bool EnsureValidClientId(ClientController clientController, MessageContext messageContext, out string error) {
            switch (messageContext.clientIdValidation) {
                case ClientIdValidation.Valid:
                    error = null;
                    return true;
                case ClientIdValidation.Invalid:
                    error = $"invalid client id. 'clt': {messageContext.clientId}";
                    return false;
                case ClientIdValidation.IsNull:
                    var user                            = messageContext.User; 
                    messageContext.clientId             = clientController.NewClientIdFor(user);
                    messageContext.clientIdValidation   = ClientIdValidation.Valid;
                    error = null;
                    return true;
            }
            throw new InvalidOperationException ("unexpected clientIdValidation state");
        }

        private async Task<Authorizer> GetAuthorizer(JsonKey userId) {
            var readPermission = userStore.permissions.Read().Find(userId);
            await userStore.SyncTasks().ConfigureAwait(false);
            UserPermission permission = readPermission.Result;
            var roles = permission.roles;
            if (roles == null || roles.Count == 0) {
                return unknown;
            }
            await AddNewRoles(roles).ConfigureAwait(false);
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
            await userStore.SyncTasks().ConfigureAwait(false);
            foreach (var newRolePair in readRoles.Results) {
                string role     = newRolePair.Key;
                Role newRole    = newRolePair.Value;
                if (newRole == null)
                    throw new InvalidOperationException($"authorization role not found: '{role}'");
                var authorizers = new List<Authorizer>(newRole.rights.Count);
                foreach (var right in newRole.rights) {
                    Authorizer authorizer;
                    if (right is RightPredicate predicates) {
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
        
        private Authorizer GetPredicatesAuthorizer(RightPredicate right) {
            var authorizers = new List<Authorizer>(right.names.Count);
            foreach (var predicateName in right.names) {
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