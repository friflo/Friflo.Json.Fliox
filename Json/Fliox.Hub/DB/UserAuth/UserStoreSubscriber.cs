// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host.Auth;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    /// <summary>
    /// Used to invalidate cached see cref="Host.Auth.Authenticator.users"/> in case:
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
            store.UserId    = UserDB.ID.AuthenticationUser;
            store.ClientId  = "user_db_subscriber";
            store.SetEventProcessor(new SynchronousEventProcessor());
            store.credentials.SubscribeChanges  (Change.All, CredentialChange);
            store.permissions.SubscribeChanges  (Change.All, PermissionChange);
            store.roles.SubscribeChanges        (Change.All, RoleChange);
            store.targets.SubscribeChanges      (Change.All, TargetChange);
            await store.SyncTasks().ConfigureAwait(false);
        }
        
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void CredentialChange(Changes<string, UserCredential> changes, EventContext context) {
            var changedUsers = new List<string>();
            changes.GetKeys(changedUsers);
            userAuthenticator.InvalidateUsers(changedUsers);
        }
        
        private void PermissionChange(Changes<string, UserPermission> changes, EventContext context) {
            var changedUsers = new List<string>();
            changes.GetKeys(changedUsers);
            userAuthenticator.InvalidateUsers(changedUsers);
        }
        
        private void RoleChange(Changes<string, Role> changes, EventContext context) {
            var roles    = new List<string>();
            changes.GetKeys(roles);
            
            var changedRoles    = roles.ToArray();
            var affectedUsers   = new HashSet<ShortString>(ShortString.Equality);
            foreach (var changedRole in changedRoles) {
                if(!userAuthenticator.roleCache.TryRemove(changedRole, out _))
                    continue;
                AddAffectedUsers(affectedUsers, changedRoles);
            }
            userAuthenticator.InvalidateUsers(affectedUsers);
        }
        
        private void TargetChange(Changes<string, UserTarget> changes, EventContext context) {
            var dispatcher  = userAuthenticator.userHub.EventDispatcher;
            var users       = userAuthenticator.users;
            foreach (var upsert in changes.Upserts) {
                var userTarget = upsert.entity;
                dispatcher.UpdateSubUserGroups(userTarget.id, userTarget.groups);
                if (!users.TryGetValue(new ShortString(userTarget.id), out User user))
                    continue;
                user.SetGroups(userTarget.groups);
            }
            foreach (var delete in changes.Deletes) {
                dispatcher.UpdateSubUserGroups(delete.key, null);
                if (!users.TryGetValue(new ShortString(delete.key), out User user))
                    continue;
                user.SetGroups(null);
            }
        }
        
        /// Iterate all users and add those to <paramref name="affectedUsers"/> having any of the given <paramref name="roles"/>.
        private void AddAffectedUsers(HashSet<ShortString> affectedUsers, string[] roles) {
            foreach (var pair in userAuthenticator.users) {
                var user = pair.Value;
                if (IsIntersectionEmpty(user.roles, roles)) {
                    continue;
                }
                affectedUsers.Add(user.userId);
            }
        }
        
        private static bool IsIntersectionEmpty (string[] rolesLeft, string[] rolesRight) {
            if (rolesLeft == null) {
                return true;
            }
            foreach (var roleRight in rolesRight) {
                if (rolesLeft.Contains(roleRight)) {
                    return false;
                }
            }
            return true;
        }
    }
}