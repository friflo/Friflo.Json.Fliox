// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;
using Friflo.Json.Fliox.Hub.Host.Event;
using static Friflo.Json.Fliox.Hub.DB.UserAuth.UserStore.ID;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    public enum Users
    {
        All,
        Authenticated
    }
    
    public static class UserAuthExtensions
    {
        /// <summary>
        /// Set default admin permissions records in the user database to enable Hub access with user admin.<br/>
        /// </summary>
        /// <remarks>
        /// - create user credential: <b>admin</b> if not exist<br/>
        /// - set user permission: <b>admin</b> with role <b>hub-admin</b><br/>
        /// - set role: <b>hub-admin</b> granting full access to all databases<br/>
        /// <br/>
        /// This enables access to all Hub databases as user <b>admin</b> without accessing the user database directly.
        /// </remarks>
        public static async Task SetAdminPermissions(this UserAuthenticator userAuthenticator, string token = "admin") {
            var userStore = new UserStore(userAuthenticator.userHub) { UserId = UserDB.ID.Server };
            userStore.Options.WritePretty   = true;
            
            var adminCredential     = new UserCredential { id = Admin, token   = token };
            userStore.credentials.Create(adminCredential);
            
            var adminPermission     = new UserPermission { id = Admin, roles   = new HashSet<string> { HubAdmin } };
            var hubAdmin            = new Role {
                id          = HubAdmin,
                taskRights  = new List<TaskRight> { new DbFullRight { database = "*"} },
                hubRights   = new HubRights { queueEvents = true },
                description = "Grant unrestricted access to all databases"
            };
            var upsertPermission    = userStore.permissions.Upsert(adminPermission);
            var upsertRole          = userStore.roles.Upsert(hubAdmin);
            var sync = await userStore.TrySyncTasks().ConfigureAwait(false);
            if (upsertPermission.Success && upsertRole.Success) {
                return;                
            }
            throw new InvalidOperationException($"Failed writing default permissions. error: {sync.Message}");
        }

        public static async Task SetClusterPermissions(this UserAuthenticator userAuthenticator, string clusterDB, Users users) {
            var userStore = new UserStore(userAuthenticator.userHub) { UserId = UserDB.ID.Server };
            userStore.Options.WritePretty   = true;

            var readPermissions = userStore.permissions.Read();
            var findAllUsers            = readPermissions.Find(AllUsers);
            var findAuthenticatedUsers  = readPermissions.Find(AuthenticatedUsers);
            await userStore.SyncTasks().ConfigureAwait(false);
            
            var allPermission             = findAllUsers.Result             ?? new UserPermission { id = AllUsers, roles = new HashSet<string>()};
            var authenticatedPermission   = findAuthenticatedUsers.Result   ?? new UserPermission { id = AuthenticatedUsers, roles = new HashSet<string>() };
            
            if (users == Users.All) {
                allPermission.roles.Add(ClusterInfo);
            } else {
                allPermission.roles.Remove(ClusterInfo);
                authenticatedPermission.roles.Add(ClusterInfo);
            }
            var clusterInfo             = new Role {
                id          = ClusterInfo,
                taskRights  = new List<TaskRight> { new DbFullRight { database = clusterDB} },
                description = "Allow reading the cluster database"
            };
            userStore.permissions.Upsert(allPermission);
            userStore.permissions.Upsert(authenticatedPermission);
            userStore.roles.Upsert(clusterInfo);

            await userStore.SyncTasks().ConfigureAwait(false);
        }
        
        /// <summary>
        /// Subscribe changes to <b>user_db</b> <see cref="UserStore.permissions"/> and <see cref="UserStore.roles"/> to 
        /// applying these changes to users instantaneously.<br/>
        /// <br/>
        /// Without subscribing <b>user_db</b> changes they are effective after a call to <see cref="UserStore.ClearAuthCache"/>
        /// or after a server restart.
        /// </summary>
        public static async Task SubscribeUserDbChanges(this UserAuthenticator userAuthenticator, EventDispatcher eventDispatcher) {
            userAuthenticator.userHub.EventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            var subscriber          = new UserStoreSubscriber(userAuthenticator);
            await subscriber.SetupSubscriptions ().ConfigureAwait(false);
        }
        
        public static async Task<List<string>> ValidateUserDb(this UserAuthenticator userAuthenticator, HashSet<string> databases) {
            var predicates  = userAuthenticator.GetRegisteredPredicates();
            var errors      = new List<string>();
            using(var userStore = new UserStore (userAuthenticator.userHub)) {
                userStore.UserId = UserDB.ID.AuthenticationUser;
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
                    var error = $"role not found. role: '{role}' in permission: '{permission.id}'";
                    errors.Add(error);
                }
            }
        }
        
        private static void ValidateRoles(UserStore userStore, RoleValidation roleValidation) {
            var roles = userStore.roles.Local.Entities;
            foreach (var role in roles) {
                var validation = new RoleValidation(roleValidation, role);
                foreach (var taskRight in role.taskRights) {
                    taskRight.Validate(validation);
                }
            }
        }
    }
}