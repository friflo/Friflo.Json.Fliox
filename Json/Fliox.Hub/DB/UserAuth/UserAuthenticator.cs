// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    internal class AuthCred {
        internal readonly   string          token;
        
        internal AuthCred (string token) {
            this.token  = token;
        }
    }
    
    public interface IUserAuth {
        Task<AuthResult> Authenticate(Credentials value);
    }
    
    /// <summary>
    /// Performs user authentication by validating the "userId" and the "token" assigned to a <see cref="Client.FlioxClient"/>
    /// <br></br>
    /// If authentication succeed it set the <see cref="AuthState.authorizer"/> derived from the roles assigned to the user.
    /// If authentication fails the given default <see cref="Authorizer"/> is used for the user.
    /// </summary>
    public class UserAuthenticator : Authenticator, IDisposable
    {
        // --- private / internal
        private   readonly  FlioxHub                                    userHub;
        private   readonly  IUserAuth                                   userAuth;
        private   readonly  Authorizer                                  anonymousAuthorizer;
        private   readonly  ConcurrentDictionary<string, Authorizer>    authorizerByRole = new ConcurrentDictionary <string, Authorizer>();

        public UserAuthenticator (EntityDatabase userDatabase, SharedEnv env = null, IUserAuth userAuth = null, Authorizer anonymousAuthorizer = null)
            : base (anonymousAuthorizer)
        {
            if (!(userDatabase.handler is UserDBHandler))
                throw new InvalidOperationException("userDatabase requires a handler of Type: " + nameof(UserDBHandler));
            var typeSchema          = NativeTypeSchema.Create(typeof(UserStore));
            userDatabase.Schema     = new DatabaseSchema(typeSchema);
            userHub        	        = new FlioxHub(userDatabase, env);
            userHub.Authenticator   = new UserDatabaseAuthenticator(userDatabase.name);  // authorize access to userDatabase
            this.userAuth           = userAuth;
            this.anonymousAuthorizer= anonymousAuthorizer ?? new AuthorizeDeny();
        }
        
        public void Dispose() {
            userHub.Dispose();
        }
        
        public async Task<List<string>> ValidateRoles() {
            var errors = new List<string>();
            using(var userStore = new UserStore (userHub)) {
                userStore.UserId = UserStore.AuthenticationUser;
                var queryRoles = userStore.roles.QueryAll();
                await userStore.TrySyncTasks().ConfigureAwait(false);
                
                Dictionary<string, Role> roles = queryRoles.Result;
                foreach (var pair in roles) {
                    var role = pair.Value;
                    foreach (var right in role.rights) {
                        if (!(right is PredicateRight rightPredicates))
                            break;
                        foreach (var predicateName in rightPredicates.names) {
                            if (!registeredPredicates.ContainsKey(predicateName)) {
                                var error = $"unknown predicate: '{predicateName}' in role: {role.id}";
                                errors.Add(error);
                            }
                        }
                    }
                }
                return  errors;
            }
        }

        private const string InvalidUserToken = "Authentication failed";
        
        public override async Task Authenticate(SyncRequest syncRequest, ExecuteContext executeContext)
        {
            var userId = syncRequest.userId;
            if (userId.IsNull()) {
                executeContext.AuthenticationFailed(anonymousUser, "user authentication requires 'user' id", anonymousAuthorizer);
                return;
            }
            var token = syncRequest.token;
            if (token == null) {
                executeContext.AuthenticationFailed(anonymousUser, "user authentication requires 'token'", anonymousAuthorizer);
                return;
            }
            if (users.TryGetValue(userId, out User user)) {
                if (user.token != token) {
                    executeContext.AuthenticationFailed(user, InvalidUserToken, anonymousAuthorizer);
                    return;
                }
                executeContext.AuthenticationSucceed(user, user.authorizer);
                return;
            }
            var command = new Credentials { userId = userId, token = token };
            
            // Note 1: UserStore could be created in smaller scope
            // Note 2: UserStore could be cached. This requires a FlioxClient.ClearEntities()
            // UserStore is not thread safe, create new one per Authenticate request.
            var pool = executeContext.pool;
            using (var pooled = pool.Type(() => new UserStore (userHub)).Get()) {
                var userStore = pooled.instance;
                userStore.UserId = UserStore.AuthenticationUser;
                var auth    = userAuth ?? userStore;
                var result  = await auth.Authenticate(command).ConfigureAwait(false);
                
                if (result.isValid) {
                    var authCred    = new AuthCred(token);
                    var authorizer  = await GetAuthorizer(userStore, userId).ConfigureAwait(false);
                    if (!authorizer.Success) {
                        executeContext.AuthenticationFailed(anonymousUser, authorizer.error, anonymousAuthorizer);
                        return;
                    }
                    user        = new User (userId, authCred.token, authorizer.value);
                    users.TryAdd(userId, user);
                }
                
                if (user == null || token != user.token) {
                    executeContext.AuthenticationFailed(anonymousUser, InvalidUserToken, anonymousAuthorizer);
                    return;
                }
                executeContext.AuthenticationSucceed(user, user.authorizer);
            }
        }
        
        public override ClientIdValidation ValidateClientId(ClientController clientController, ExecuteContext executeContext) {
            ref var clientId = ref executeContext.clientId; 
            if (clientId.IsNull()) {
                return ClientIdValidation.IsNull;
            }
            if (!executeContext.Authenticated) {
                return ClientIdValidation.Invalid;
            }
            var user        = executeContext.User;
            var userClients = user.clients; 
            if (userClients.ContainsKey(clientId)) {
                return ClientIdValidation.Valid;
            }
            /* // Is clientId already used?
            if (clientController.Clients.TryGetValue(clientId, out var userClient)) {
                if (!userClient.userId.IsEqual(user.userId))
                    return ClientIdValidation.Invalid;
            } */
            if (clientController.UseClientIdFor(user, clientId)) {
                userClients.TryAdd(clientId, new Empty());
                return ClientIdValidation.Valid;
            }
            return ClientIdValidation.Invalid;
        }
        
        public override bool EnsureValidClientId(ClientController clientController, ExecuteContext executeContext, out string error) {
            switch (executeContext.clientIdValidation) {
                case ClientIdValidation.Valid:
                    error = null;
                    return true;
                case ClientIdValidation.Invalid:
                    error = $"invalid client id. 'clt': {executeContext.clientId}";
                    return false;
                case ClientIdValidation.IsNull:
                    var user                            = executeContext.User; 
                    executeContext.clientId             = clientController.NewClientIdFor(user);
                    executeContext.clientIdValidation   = ClientIdValidation.Valid;
                    error = null;
                    return true;
            }
            throw new InvalidOperationException ("unexpected clientIdValidation state");
        }

        private async Task<Result<Authorizer>> GetAuthorizer(UserStore userStore, JsonKey userId) {
            var readPermission = userStore.permissions.Read().Find(userId);
            await userStore.SyncTasks().ConfigureAwait(false);
            
            UserPermission permission = readPermission.Result;
            var roles = permission.roles;
            if (roles == null || roles.Count == 0) {
                return anonymousAuthorizer;
            }
            var error = await AddNewRoles(userStore, roles).ConfigureAwait(false);
            if (error != null)
                return error;
            
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
        
        private async Task<string> AddNewRoles(UserStore userStore, List<string> roles) {
            var newRoles = new List<string>();
            foreach (var role in roles) {
                if (!authorizerByRole.TryGetValue(role, out _)) {
                    newRoles.Add(role);
                }
            }
            if (newRoles.Count == 0)
                return null;
            var readRoles = userStore.roles.Read().FindRange(newRoles);
            await userStore.SyncTasks().ConfigureAwait(false);
            
            foreach (var newRolePair in readRoles.Result) {
                string role     = newRolePair.Key;
                Role newRole    = newRolePair.Value;
                if (newRole == null)
                    return $"authorization role not found: '{role}'";
                var authorizers = new List<Authorizer>(newRole.rights.Count);
                foreach (var right in newRole.rights) {
                    Authorizer authorizer;
                    if (right is PredicateRight predicates) {
                        var result = GetPredicatesAuthorizer(predicates);
                        if (!result.Success)
                            return $"{result.error} in role: {newRole.id}";
                        authorizer = result.value;
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
            return null;
        }
        
        private Result<Authorizer> GetPredicatesAuthorizer(PredicateRight right) {
            var authorizers = new List<Authorizer>(right.names.Count);
            foreach (var predicateName in right.names) {
                if (!registeredPredicates.TryGetValue(predicateName, out var predicate)) {
                    return $"unknown predicate: {predicateName}";
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