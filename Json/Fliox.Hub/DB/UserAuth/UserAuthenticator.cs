// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Schema.Native;

// ReSharper disable UseObjectOrCollectionInitializer
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
    /// Used to store the rights given by <see cref="Role.taskRights"/> and <see cref="Role.hubRights"/>
    /// </summary>
    internal class RoleRights
    {
        /// <summary> assigned by <see cref="Role.taskRights"/> </summary>
        internal TaskAuthorizer[]   taskAuthorizers;
        /// <summary> assigned by <see cref="Role.hubRights"/> </summary>
        internal HubPermission      hubPermission;
    }

    /// <summary>
    /// Performs user authentication by validating the "userId" and the "token" assigned to a <see cref="Client.FlioxClient"/>
    /// </summary>
    /// <remarks>
    /// If authentication succeed it set the <see cref="AuthState.taskAuthorizer"/> derived from the roles assigned to the user. <br/>
    /// If authentication fails the given default <see cref="TaskAuthorizer"/> is used for the user.
    /// <br/>
    /// <b>Note:</b> User permissions and roles are cached for successful authenticated users.<br/>
    /// This enables instant task authorization and reduces the number of reads to the <b>user_db</b> significant.
    /// </remarks> 
    public sealed class UserAuthenticator : Authenticator, IDisposable
    {
        // --- private / internal
        internal  readonly  FlioxHub                                    userHub;
        private   readonly  IUserAuth                                   userAuth;
        internal  readonly  ConcurrentDictionary<string, RoleRights>    roleCache = new ConcurrentDictionary <string, RoleRights>();

        public UserAuthenticator (EntityDatabase userDatabase, SharedEnv env = null, IUserAuth userAuth = null)
        {
            if (!(userDatabase.handler is UserDBHandler))
                throw new ArgumentException("userDatabase requires a handler of Type: " + nameof(UserDBHandler));
            var sharedEnv           = env  ?? SharedEnv.Default;
            var typeSchema          = NativeTypeSchema.Create(typeof(UserStore));
            userDatabase.Schema     = new DatabaseSchema(typeSchema);
            userHub        	        = new FlioxHub(userDatabase, sharedEnv);
            userHub.Authenticator   = new UserDatabaseAuthenticator(userDatabase.name);  // authorize access to userDatabase
            this.userAuth           = userAuth;
        }
        
        public void Dispose() {
            userHub.Dispose();
        }
        
        /// <summary>
        /// Subscribe changes to <b>user_db</b> <see cref="UserStore.permissions"/> and <see cref="UserStore.roles"/> to 
        /// applying these changes to users instantaneously.<br/>
        /// <br/>
        /// Without subscribing <b>user_db</b> changes they are effective after a call to <see cref="UserStore.ClearAuthCache"/>
        /// or after a server restart.
        /// </summary>
        public UserAuthenticator SubscribeUserDbChanges(EventDispatcher eventDispatcher) {
            userHub.EventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            var subscriber          = new UserStoreSubscriber(this);
            subscriber.SetupSubscriptions ().Wait();
            return this;
        }

        public async Task<List<string>> ValidateUserDb(HashSet<string> databases) {
            var predicates  = registeredPredicates.Keys.ToHashSet();
            var errors      = new List<string>();
            using(var userStore = new UserStore (userHub)) {
                userStore.UserId = UserStore.AuthenticationUser;
                userStore.permissions.QueryAll();
                userStore.roles.QueryAll();
                var result = await userStore.TrySyncTasks().ConfigureAwait(false);
                
                if (!result.Success) {
                    foreach (var task in result.failed) {
                        errors.Add(task.Error.Message);
                    }
                    return errors;
                }
                var roleValidation = new RoleValidation(databases, predicates, errors);
                ValidatePermissions (userStore, errors);
                ValidateRoles       (userStore, roleValidation);
                return  errors;
            }
        }
        
        private static void ValidatePermissions(UserStore userStore, List<string> errors) {
            var permissions = userStore.permissions.Local.Entities;
            foreach (var permission in permissions) {
                foreach (var role in permission.roles) {
                    if (userStore.roles.Local.ContainsKey(role))
                        continue;
                    var error = $"role not found. role: '{role}' in permission: {permission.id}";
                    errors.Add(error);
                }
            }
        }
        
        private void ValidateRoles(UserStore userStore, RoleValidation roleValidation) {
            var roles = userStore.roles.Local.Entities;
            foreach (var role in roles) {
                var validation = new RoleValidation(roleValidation, role);
                foreach (var taskRight in role.taskRights) {
                    taskRight.Validate(validation);
                }
            }
        }

        private const string InvalidUserToken = "Authentication failed";
        
        public override async Task Authenticate(SyncRequest syncRequest, SyncContext syncContext)
        {
            var userId = syncRequest.userId;
            if (userId.IsNull()) {
                syncContext.AuthenticationFailed(anonymousUser, "user authentication requires 'user' id", AnonymousTaskAuthorizer, AnonymousHubPermission );
                return;
            }
            var token = syncRequest.token;
            if (token == null) {
                syncContext.AuthenticationFailed(anonymousUser, "user authentication requires 'token'", AnonymousTaskAuthorizer, AnonymousHubPermission);
                return;
            }
            if (users.TryGetValue(userId, out User user)) {
                if (user.token != token) {
                    syncContext.AuthenticationFailed(user, InvalidUserToken, AnonymousTaskAuthorizer, AnonymousHubPermission);
                    return;
                }
                syncContext.AuthenticationSucceed(user, user.taskAuthorizer, user.hubPermission);
                return;
            }
            var command = new Credentials { userId = userId, token = token };
            
            // Note 1: UserStore could be created in smaller scope
            // Note 2: UserStore could be cached. This requires a FlioxClient.ClearEntities()
            // UserStore is not thread safe, create new one per Authenticate request.
            var pool = syncContext.pool;
            using (var pooled = pool.Type(() => new UserStore (userHub)).Get()) {
                var userStore = pooled.instance;
                userStore.UserId = UserStore.AuthenticationUser;
                var auth    = userAuth ?? userStore;
                var result  = await auth.Authenticate(command).ConfigureAwait(false);
                
                if (result.isValid) {
                    var authCred        = new AuthCred(token);
                    var userAuthInfo    = await GetUserAuthInfo(userStore, userId).ConfigureAwait(false);
                    if (!userAuthInfo.Success) {
                        syncContext.AuthenticationFailed(anonymousUser, userAuthInfo.error, AnonymousTaskAuthorizer, AnonymousHubPermission);
                        return;
                    }
                    var ua  = userAuthInfo.value;
                    user    = new User (userId, authCred.token) { taskAuthorizer = ua.taskAuthorizer, hubPermission = ua.hubPermission };
                    user.SetGroups(userAuthInfo.value.groups);
                    users.TryAdd(userId, user);
                }
                
                if (user == null || token != user.token) {
                    syncContext.AuthenticationFailed(anonymousUser, InvalidUserToken, AnonymousTaskAuthorizer, AnonymousHubPermission);
                    return;
                }
                syncContext.AuthenticationSucceed(user, user.taskAuthorizer, user.hubPermission);
            }
        }
        
        public override ClientIdValidation ValidateClientId(ClientController clientController, SyncContext syncContext) {
            ref var clientId = ref syncContext.clientId; 
            if (clientId.IsNull()) {
                return ClientIdValidation.IsNull;
            }
            if (!syncContext.Authenticated) {
                return ClientIdValidation.Invalid;
            }
            var user        = syncContext.User;
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
        
        public override bool EnsureValidClientId(ClientController clientController, SyncContext syncContext, out string error) {
            switch (syncContext.clientIdValidation) {
                case ClientIdValidation.Valid:
                    error = null;
                    return true;
                case ClientIdValidation.Invalid:
                    error = $"invalid client id. 'clt': {syncContext.clientId}";
                    return false;
                case ClientIdValidation.IsNull:
                    error           = null;
                    var user        = syncContext.User; 
                    var clientId    = clientController.NewClientIdFor(user);
                    syncContext.SetClientId(clientId);
                    return true;
            }
            throw new InvalidOperationException ("unexpected clientIdValidation state");
        }
        
        public override async Task SetUserOptions (User user, UserParam param) {
            var store       = new UserStore(userHub);
            store.UserId    = UserStore.AuthenticationUser;
            var read        = store.targets.Read().Find(user.userId);
            await store.SyncTasks().ConfigureAwait(false);
            
            var userTarget      = read.Result ?? new UserTarget { id = user.userId, groups = new List<string>() };
            var groups          = User.UpdateGroups(userTarget.groups, param);
            userTarget.groups   = groups.ToList();
            store.targets.Upsert(userTarget);
            await store.SyncTasks().ConfigureAwait(false);

            await base.SetUserOptions(user, param).ConfigureAwait(false); 
        }

        private async Task<Result<UserAuthInfo>> GetUserAuthInfo(UserStore userStore, JsonKey userId) {
            var readPermission  = userStore.permissions.Read().Find(userId);
            var readTarget      = userStore.targets.Read().Find(userId);
            await userStore.SyncTasks().ConfigureAwait(false);
            
            var targetGroups    = readTarget.Result?.groups;
            UserPermission permission = readPermission.Result;
            var roleNames = permission.roles;
            if (roleNames == null || roleNames.Count == 0) {
                return new UserAuthInfo(new [] { AnonymousTaskAuthorizer }, new[] { AnonymousHubPermission }, targetGroups);
            }
            var error = await AddNewRoles(userStore, roleNames).ConfigureAwait(false);
            if (error != null)
                return error;
            
            var taskAuthorizers = new List<TaskAuthorizer>(roleNames.Count);
            var hubPermissions  = new List<HubPermission>(roleNames.Count);
            foreach (var roleName in roleNames) {
                // existence is checked already in AddNewRoles()
                if (!roleCache.TryGetValue(roleName, out var role))
                    throw new InvalidOperationException($"roleAuthorizers not found: {roleName}");
                taskAuthorizers.AddRange(role.taskAuthorizers);
                hubPermissions.Add(role.hubPermission);
            }
            return new UserAuthInfo(taskAuthorizers, hubPermissions, targetGroups);
        }
        
        private async Task<string> AddNewRoles(UserStore userStore, List<string> roles) {
            var newRoles = new List<string>();
            foreach (var role in roles) {
                if (!roleCache.TryGetValue(role, out _)) {
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
                var taskAuthorizers = new List<TaskAuthorizer>(newRole.taskRights.Count);
                foreach (var taskRight in newRole.taskRights) {
                    TaskAuthorizer authorizer;
                    if (taskRight is PredicateRight predicates) {
                        var result = GetPredicatesAuthorizer(predicates);
                        if (!result.Success)
                            return $"{result.error} in role: {newRole.id}";
                        authorizer = result.value;
                    } else {
                        authorizer = taskRight.ToAuthorizer();
                    }
                    taskAuthorizers.Add(authorizer);
                }
                var hubPermission   = new HubPermission (newRole.hubRights?.queueEvents == true);
                var roleRights      = new RoleRights { taskAuthorizers = taskAuthorizers.ToArray(), hubPermission = hubPermission };
                roleCache.TryAdd(role, roleRights);
            }
            return null;
        }
        
        private Result<TaskAuthorizer> GetPredicatesAuthorizer(PredicateRight right) {
            var authorizers = new List<TaskAuthorizer>(right.names.Count);
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
    
    internal class UserAuthInfo
    {
        internal readonly   TaskAuthorizer  taskAuthorizer;
        internal readonly   HubPermission   hubPermission;
        internal readonly   List<string>    groups;
        
        internal UserAuthInfo(IList<TaskAuthorizer> authorizers, IList<HubPermission> hubPermissions, List<string> groups) {
            var taskAuthorizers = new List<TaskAuthorizer>();
            foreach (var authorizer in authorizers) {
                switch (authorizer) {
                    case AuthorizeDatabase _:
                    case AuthorizeTaskType _:
                    case AuthorizeContainer _:
                    case AuthorizeSendMessage _:
                    case AuthorizeSubscribeMessage _:
                    case AuthorizeSubscribeChanges _:
                    case AuthorizePredicate _:
                    case AuthorizeAny _:
                        taskAuthorizers.Add(authorizer);
                        break;
                    case AuthorizeDeny _:
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected authorizer: {authorizer}");
                }
            }
            this.taskAuthorizer = TaskAuthorizer.ToAuthorizer(taskAuthorizers);
            this.groups         = groups;
            
            bool queueEvents = false;
            foreach (var permission in hubPermissions) {
                queueEvents |= permission.queueEvents;
            }
            hubPermission  = new HubPermission(queueEvents);
        }
    }
}