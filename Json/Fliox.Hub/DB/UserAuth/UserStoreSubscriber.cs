// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    public class UserStoreSubscriber : SubscriptionProcessor
    {
        private readonly UserStore              client;
        private readonly UserAuthenticator      userAuthenticator;
        
        public UserStoreSubscriber(UserStore client, UserAuthenticator userAuthenticator)
            : base(client)
        {
            this.client             = client;
            this.userAuthenticator  = userAuthenticator;
        }
        
        public static void CreateSubscriber(UserAuthenticator userAuthenticator) {
            var userHub     = userAuthenticator.userHub;
            userHub.EventBroker = new EventBroker(true);
            var changes     = new [] { Change.create, Change.upsert, Change.delete, Change.patch };
            var store       = new UserStore (userHub);
            store.UserId    = UserStore.AuthenticationUser;
            store.ClientId  = "user_db_subscriber";
            var subscriber  = new UserStoreSubscriber(store, userAuthenticator);
            store.SetSubscriptionProcessor(subscriber);
            store.permissions.SubscribeChanges(changes);
            store.roles.SubscribeChanges(changes);
            store.SyncTasks().Wait();
        }
        
        /// <summary>
        /// used to remove users from cached <see cref="UserAuthenticator.users"/> in case:
        /// - a user permission changes
        /// - a role assigned to a user changes
        /// </summary>
        public override void EnqueueEvent(EventMessage ev) {
            ProcessEvent(ev);

            var credentialChanges   = GetEntityChanges(client.credentials,  ev);
            var permissionChanges   = GetEntityChanges(client.permissions,  ev);
            var changedUsers = new HashSet<JsonKey>(JsonKey.Equality);
            
            foreach (var pair   in credentialChanges.upserts) { changedUsers.Add(pair.Key); }
            foreach (var id     in credentialChanges.deletes) { changedUsers.Add(id); }
            foreach (var pair   in credentialChanges.patches) { changedUsers.Add(pair.Key); }

            foreach (var pair   in permissionChanges.upserts) { changedUsers.Add(pair.Key); }
            foreach (var id     in permissionChanges.deletes) { changedUsers.Add(id); }
            foreach (var pair   in permissionChanges.patches) { changedUsers.Add(pair.Key); }
            
            var users = userAuthenticator.users;
            foreach (var changedUser in changedUsers) {
                users.Remove(changedUser, out _);
            }
            
            var roleChanges     = GetEntityChanges(client.roles,        ev);
            var changedRoles    = new HashSet<string>();
            
            foreach (var pair   in roleChanges.upserts) { changedRoles.Add(pair.Key); }
            foreach (var id     in roleChanges.deletes) { changedRoles.Add(id); }
            foreach (var pair   in roleChanges.patches) { changedRoles.Add(pair.Key); }
            
            foreach (var changedRole in changedRoles) {
                if (!userAuthenticator.roleUserCache.TryGetValue(changedRole, out var roleUser))
                    continue;
                foreach (var userId in roleUser.users) {
                    users.Remove(userId, out _);    
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