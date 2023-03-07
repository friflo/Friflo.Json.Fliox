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

// ReSharper disable InlineTemporaryVariable
// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    public interface IUserAuth {
        Task<AuthResult> AuthenticateAsync(Credentials value);
    }

    /// <summary>
    /// Used to store the rights given by <see cref="Role.taskRights"/> and <see cref="Role.hubRights"/>
    /// </summary>
    internal sealed class RoleRights
    {
        /// <summary> assigned by <see cref="Role.taskRights"/> </summary>
        internal readonly   TaskAuthorizer[]    taskAuthorizers;
        /// <summary> assigned by <see cref="Role.hubRights"/> </summary>
        internal readonly   HubPermission       hubPermission;
        
        internal RoleRights(TaskAuthorizer[] taskAuthorizers, HubPermission hubPermission) {
            this.taskAuthorizers    = taskAuthorizers;
            this.hubPermission      = hubPermission;
        }
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
        private   readonly  User                                        anonymous;

        public UserAuthenticator (EntityDatabase userDatabase, SharedEnv env = null, IUserAuth userAuth = null)
        {
            if (!(userDatabase.service is UserDBService))
                throw new ArgumentException("userDatabase requires a handler of Type: " + nameof(UserDBService));
            var sharedEnv           = env  ?? SharedEnv.Default;
            userDatabase.Schema     = new DatabaseSchema(typeof(UserStore));
            userHub        	        = new FlioxHub(userDatabase, sharedEnv);
            userHub.Authenticator   = new UserDatabaseAuthenticator(userDatabase.name);  // authorize access to userDatabase
            this.userAuth           = userAuth;
            anonymous               = new User(User.AnonymousId);
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
        
        internal void InvalidateUsers(IEnumerable<ShortString> userIds) {
            foreach (var userId in userIds) {
                if (!users.TryGetValue(userId, out var user))
                    continue;
                user.invalidated = true;
            }
        }

        // --- Authenticator 
        /// <summary>
        /// <see cref="Authenticate"/> can run synchronous if already successful authenticated or a general
        /// authentication error occured.
        /// </summary>
        public override bool IsSynchronous(SyncRequest syncRequest) {
            return PreAuth(syncRequest, out syncRequest.intern.preAuthType, out syncRequest.intern.preAuthUser);
        }
        
        public override void Authenticate (SyncRequest syncRequest, SyncContext syncContext) {
            bool isSync = AuthenticateSynchronous(syncRequest.intern.preAuthType, syncRequest.intern.preAuthUser, syncContext);
            if (isSync)
                return;
            throw new InvalidOperationException("authentication cannot be executed synchronously");
        }
        
        /// <summary>returns true if authentication can be executed synchronously</summary>
        private bool PreAuth(SyncRequest syncRequest, out PreAuthType type, out User user) {
            if (syncRequest.userId.IsNull()) {
                user = null;
                type = PreAuthType.MissingUserId;
                return !anonymous.invalidated;
            }
            if (syncRequest.token.IsNull()) {
                user = null;
                type = PreAuthType.MissingToken;
                return !anonymous.invalidated;
            }
            if (users.TryGetValue(syncRequest.userId, out user)) {
                if (!user.token.IsEqual(syncRequest.token)) {
                    type = PreAuthType.Failed;
                    return false;
                }
                if (user.invalidated) {
                    type = PreAuthType.UserInvalidated;
                    return false;
                }
                type = PreAuthType.Success;
                return true;
            }
            type = PreAuthType.UserUnknown;
            return false;
        }
        
        private const string InvalidUserToken = "Authentication failed";
        
        private bool AuthenticateSynchronous(PreAuthType type, User user, SyncContext syncContext) {
            var anon = anonymous;
            switch (type) {
                case PreAuthType.MissingUserId:
                    syncContext.AuthenticationFailed(anon, "user authentication requires 'user' id", anon.taskAuthorizer, anon.hubPermission);
                    return true;
                case PreAuthType.MissingToken:
                    syncContext.AuthenticationFailed(anon, "user authentication requires 'token'", anon.taskAuthorizer, anon.hubPermission);
                    return true;
                case PreAuthType.Failed:
                    syncContext.AuthenticationFailed(user, InvalidUserToken, anon.taskAuthorizer, anon.hubPermission);
                    return true;
                case PreAuthType.Success:
                    syncContext.AuthenticationSucceed(user, user.taskAuthorizer, user.hubPermission);
                    return true;
            }
            return false;
        }

        public override async Task AuthenticateAsync(SyncRequest syncRequest, SyncContext syncContext)
        {
            // Note: UserStore could be cached. This requires a FlioxClient.ClearEntities()
            // UserStore is not thread safe, create new one per Authenticate request.
            using (var pooled = syncContext.pool.Type(() => new UserStore (userHub)).Get()) {
                var userStore       = pooled.instance;
                userStore.UserId    = UserStore.AuthenticationUser;
                var type            = syncRequest.intern.preAuthType;
                var user            = syncRequest.intern.preAuthUser;
                switch (type) {
                    case PreAuthType.MissingUserId:
                    case PreAuthType.MissingToken:
                    case PreAuthType.Failed:
                        await GetAnonymousUserAuthAsync(userStore); // ensure anonymous is set 
                        AuthenticateSynchronous(type, user, syncContext);
                        return;
                    case PreAuthType.UserUnknown:
                    case PreAuthType.UserInvalidated:
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected PreAuthType type: {type}");
                }
                var auth    = userAuth ?? userStore;
                var userId  = syncRequest.userId;
                var token   = syncRequest.token;
                var command = new Credentials { userId = userId, token = token };
                var result  = await auth.AuthenticateAsync(command).ConfigureAwait(false);
                
                if (result.isValid) {
                    var userAuthInfo    = await GetUserAuthInfoAsync(userStore, userId).ConfigureAwait(false);
                    if (!userAuthInfo.Success) {
                        var anon = await GetAnonymousUserAuthAsync(userStore);
                        syncContext.AuthenticationFailed(anon, userAuthInfo.error, anon.taskAuthorizer, anon.hubPermission);
                        return;
                    }
                    var ua  = userAuthInfo.value;
                    user ??= new User(userId);
                    user.Set(token, ua.taskAuthorizer, ua.hubPermission, ua.roles);
                    user.SetGroups(ua.groups);
                    users.TryAdd(userId, user);
                }
                if (user == null || !token.IsEqual(user.token)) {
                    var anon = await GetAnonymousUserAuthAsync(userStore);
                    syncContext.AuthenticationFailed(anon, InvalidUserToken, anon.taskAuthorizer, anon.hubPermission);
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
        
        public override async Task SetUserOptionsAsync (User user, UserParam param) {
            var store       = new UserStore(userHub);
            store.UserId    = UserStore.AuthenticationUser;
            var read        = store.targets.Read().Find(user.userId);
            await store.SyncTasks().ConfigureAwait(false);
            
            var userTarget      = read.Result ?? new UserTarget { id = user.userId, groups = new List<ShortString>() };
            var groups          = User.UpdateGroups(userTarget.groups, param);
            userTarget.groups   = groups.ToList();
            store.targets.Upsert(userTarget);
            await store.SyncTasks().ConfigureAwait(false);

            await base.SetUserOptionsAsync(user, param).ConfigureAwait(false); 
        }
        
        private async Task<User> GetAnonymousUserAuthAsync(UserStore userStore) {
            var anonymousAuthInfo   = await GetUserAuthInfoAsync(userStore, User.AnonymousId);
            
            var anon = anonymous;
            users.TryAdd(anon.userId, anon);
            if (!anonymousAuthInfo.Success) {
                return anon.Set(default, TaskAuthorizer.None, HubPermission.None, null);
            }
            var ua = anonymousAuthInfo.value;
            return anon.Set(default, ua.taskAuthorizer, ua.hubPermission, ua.roles);
        }

        private async Task<Result<UserAuthInfo>> GetUserAuthInfoAsync(UserStore userStore, ShortString userId) {
            var readPermission  = userStore.permissions.Read().Find(userId);
            var readTarget      = userStore.targets.Read().Find(userId);
            await userStore.SyncTasks().ConfigureAwait(false);
            
            var targetGroups    = readTarget.Result?.groups;
            UserPermission permission = readPermission.Result;
            var roleNames = permission?.roles;
            if (roleNames == null || roleNames.Count == 0) {
                return new UserAuthInfo(ArraySegment<TaskAuthorizer>.Empty, ArraySegment<HubPermission>.Empty, targetGroups, null);
            }
            var error = await AddNewRoles(userStore, roleNames).ConfigureAwait(false);
            if (error != null)
                return error;
            
            var taskAuthorizers = new List<TaskAuthorizer>(roleNames.Count);
            var hubPermissions  = new List<HubPermission> (roleNames.Count);
            foreach (var roleName in roleNames) {
                // existence is checked already in AddNewRoles()
                if (!roleCache.TryGetValue(roleName, out var role))
                    throw new InvalidOperationException($"roleAuthorizers not found: {roleName}");
                taskAuthorizers.AddRange(role.taskAuthorizers);
                hubPermissions.Add(role.hubPermission);
            }
            return new UserAuthInfo(taskAuthorizers, hubPermissions, targetGroups, roleNames);
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
                var roleRights      = new RoleRights (taskAuthorizers.ToArray(), hubPermission);
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
}