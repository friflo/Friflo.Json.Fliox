// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    /// <summary>
    /// used to remove users from cached <see cref="Host.Auth.Authenticator.users"/> in case:
    /// - a user permission changes
    /// - a role assigned to a user changes
    /// </summary>
    internal class UserStoreSubscriber
    {
        private readonly UserAuthenticator userAuthenticator;
        
        internal UserStoreSubscriber(UserAuthenticator userAuthenticator) {
            this.userAuthenticator = userAuthenticator;
        }
            
        internal void SetupSubscriptions() {
            var change      = ChangeFlags.All;
            var store       = new UserStore (userAuthenticator.userHub);
            // userAuthenticator.userHub.EventDispatcher = new EventDispatcher(true);
            store.UserId    = UserStore.AuthenticationUser;
            store.ClientId  = "user_db_subscriber";
            store.SetEventProcessor(new DirectEventProcessor());
            // store.credentials.SubscribeChanges  (change, CredentialChange);
            store.permissions.SubscribeChanges  (change, PermissionChange);
            store.roles.SubscribeChanges        (change, RoleChange);
            store.SyncTasks().Wait();
        }
        
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
                if(!userAuthenticator.authorizerByRole.TryRemove(changedRole, out var authorizer))
                    continue;
                AddAffectedUsers(affectedUsers, authorizer);
            }
            foreach (var user in affectedUsers) {
                userAuthenticator.users.TryRemove(user, out _);
            }
        }
        
        /// Iterate all authorized users and remove those having an <see cref="Authorizer"/> which was modified.
        /// Used iteration instead of an additional map (role -> users) to avoid long lived objects in heap.
        private void AddAffectedUsers(List<JsonKey> affectedUsers, Authorizer search) {
            foreach (var pair in userAuthenticator.users) {
                var user = pair.Value;
                if (user.authorizer is AuthorizeAny any) {
                    foreach (var authorizer in any.list) {
                        if (authorizer == search) {
                            affectedUsers.Add(user.userId);
                            break;
                        }
                    }
                    continue;
                }
                if (user.authorizer == search) {
                    affectedUsers.Add(user.userId);                    
                }
            }
        }
    }
    
    internal readonly struct RoleUsers
    {
        internal readonly HashSet<JsonKey> users;
        
        internal RoleUsers(HashSet<JsonKey> users) {
            this.users = users;
        }
    }
}