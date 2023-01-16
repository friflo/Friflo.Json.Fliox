// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host.Auth;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    /// <summary>
    /// used to remove users from cached <see cref="Host.Auth.Authenticator.users"/> in case:
    /// - a user permission changes
    /// - a role assigned to a user changes
    /// </summary>
    internal sealed class UserStoreSubscriber
    {
        private readonly UserAuthenticator userAuthenticator;
        
        internal UserStoreSubscriber(UserAuthenticator userAuthenticator) {
            this.userAuthenticator = userAuthenticator;
        }
            
        internal async Task SetupSubscriptions() {
            var store       = new UserStore (userAuthenticator.userHub);
            store.UserId    = UserStore.AuthenticationUser;
            store.ClientId  = "user_db_subscriber";
            store.SetEventProcessor(new SynchronousEventProcessor());
        //  store.credentials.SubscribeChanges  (Change.All, CredentialChange);
            store.permissions.SubscribeChanges  (Change.All, PermissionChange);
            store.roles.SubscribeChanges        (Change.All, RoleChange);
            store.targets.SubscribeChanges      (Change.All, TargetChange);
            await store.SyncTasks().ConfigureAwait(false);
        }
        
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void CredentialChange(Changes<JsonKey, UserCredential> changes, EventContext context) {
            var changedUsers = new HashSet<JsonKey>(JsonKey.Equality);
            foreach (var entity in changes.Upserts) { changedUsers.Add(entity.id); }
            foreach (var id     in changes.Deletes) { changedUsers.Add(id); }
            foreach (var patch  in changes.Patches) { changedUsers.Add(patch.key); }
                
            foreach (var changedUser in changedUsers) {
                userAuthenticator.users.TryRemove(changedUser, out _);
            }
        }
        
        private void PermissionChange(Changes<JsonKey, UserPermission> changes, EventContext context) {
            var changedUsers = new HashSet<JsonKey>(JsonKey.Equality);
            foreach (var entity in changes.Upserts) { changedUsers.Add(entity.id); }
            foreach (var id     in changes.Deletes) { changedUsers.Add(id); }
            foreach (var patch  in changes.Patches) { changedUsers.Add(patch.key); }
                
            foreach (var changedUser in changedUsers) {
                userAuthenticator.users.TryRemove(changedUser, out _);
            }
        }
        
        private void RoleChange(Changes<string, Role> changes, EventContext context) {
            var changedRoles    = new List<string>();
            
            foreach (var entity in changes.Upserts) { changedRoles.Add(entity.id); }
            foreach (var id     in changes.Deletes) { changedRoles.Add(id); }
            foreach (var patch  in changes.Patches) { changedRoles.Add(patch.key); }
            
            var affectedUsers = new List<JsonKey>();
            foreach (var changedRole in changedRoles) {
                if(!userAuthenticator.roleCache.TryRemove(changedRole, out var role))
                    continue;
                AddAffectedUsers(affectedUsers, role.taskAuthorizers);
            }
            foreach (var user in affectedUsers) {
                userAuthenticator.users.TryRemove(user, out _);
            }
        }
        
        private void TargetChange(Changes<JsonKey, UserTarget> changes, EventContext context) {
            var dispatcher  = userAuthenticator.userHub.EventDispatcher;
            var users       = userAuthenticator.users;
            foreach (var userTarget in changes.Upserts) {
                dispatcher.UpdateSubUserGroups(userTarget.id, userTarget.groups);
                if (!users.TryGetValue(userTarget.id, out User user))
                    continue;
                user.SetGroups(userTarget.groups);
            }
            foreach (var id in changes.Deletes) {
                dispatcher.UpdateSubUserGroups(id, null);
                if (!users.TryGetValue(id, out User user))
                    continue;
                user.SetGroups(null);
            }
        }
        
        /// Iterate all authorized users and remove those having an <see cref="TaskAuthorizer"/> which was modified.
        /// Used iteration instead of an additional map (role -> users) to avoid long lived objects in heap.
        private void AddAffectedUsers(List<JsonKey> affectedUsers, TaskAuthorizer[] search) {
            foreach (var pair in userAuthenticator.users) {
                var user = pair.Value;
                if (user.taskAuthorizer is AuthorizeAny any) {
                    foreach (var authorizer in any.list) {
                        if (search.Contains(authorizer)) {
                            affectedUsers.Add(user.userId);
                            break;
                        }
                    }
                    continue;
                }
                if (search.Contains(user.taskAuthorizer)) {
                    affectedUsers.Add(user.userId);                    
                }
            }
        }
    }
}