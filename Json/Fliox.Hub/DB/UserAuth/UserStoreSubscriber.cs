// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    internal class UserStoreSubscriber : SubscriptionProcessor
    {
        private readonly UserStore              client;
        private readonly UserAuthenticator      userAuthenticator;
        
        private UserStoreSubscriber(UserStore client, UserAuthenticator userAuthenticator) {
            this.client             = client;
            this.userAuthenticator  = userAuthenticator;
        }
        
        internal static void CreateSubscriber(UserAuthenticator userAuthenticator, FlioxHub hub) {
            var changes     = new [] { Change.create, Change.upsert, Change.delete, Change.patch };
            var store       = new UserStore (hub, userAuthenticator.userHub.DatabaseName);
            // userAuthenticator.userHub.EventBroker = new EventBroker(true);
            store.UserId    = UserStore.AuthenticationUser;
            store.UserId    = "admin";
            store.Token     = "admin";
            store.ClientId  = "user_db_subscriber";
            var subscriber  = new UserStoreSubscriber(store, userAuthenticator);
            store.SetSubscriptionProcessor(subscriber);
            var eventProcessor = new DirectEventHandler();
            store.SetEventHandler(eventProcessor);
            store.permissions.SubscribeChanges(changes, (change, context) => {
                var changedUsers = new HashSet<JsonKey>(JsonKey.Equality);
                foreach (var pair   in change.upserts) { changedUsers.Add(pair.Key); }
                foreach (var id     in change.deletes) { changedUsers.Add(id); }
                foreach (var pair   in change.patches) { changedUsers.Add(pair.Key); }
                
                foreach (var changedUser in changedUsers) {
                    userAuthenticator.users.TryRemove(changedUser, out _);
                }
            });
            store.credentials.SubscribeChanges(changes, (change, context) => {
                var changedUsers = new HashSet<JsonKey>(JsonKey.Equality);
                foreach (var pair   in change.upserts) { changedUsers.Add(pair.Key); }
                foreach (var id     in change.deletes) { changedUsers.Add(id); }
                foreach (var pair   in change.patches) { changedUsers.Add(pair.Key); }
                
                foreach (var changedUser in changedUsers) {
                    userAuthenticator.users.TryRemove(changedUser, out _);
                }
            });
            store.roles.SubscribeChanges(changes);
            store.SyncTasks().Wait();
        }
        
        /// <summary>
        /// used to remove users from cached <see cref="Host.Auth.Authenticator.users"/> in case:
        /// - a user permission changes
        /// - a role assigned to a user changes
        /// </summary>
        public override void OnEvent(FlioxClient c, EventMessage ev) {
            if (!ReferenceEquals(client, c))    throw new InvalidOperationException("unexpected client reference");
            ProcessEvent(client, ev);

            var roleChanges     = GetEntityChanges(client.roles);
            var changedRoles    = new HashSet<string>();
            
            foreach (var pair   in roleChanges.upserts) { changedRoles.Add(pair.Key); }
            foreach (var id     in roleChanges.deletes) { changedRoles.Add(id); }
            foreach (var pair   in roleChanges.patches) { changedRoles.Add(pair.Key); }
            
            foreach (var changedRole in changedRoles) {
                if (!userAuthenticator.roleUserCache.TryGetValue(changedRole, out var roleUser))
                    continue;
                foreach (var userId in roleUser.users) {
                    userAuthenticator.users.TryRemove(userId, out _);    
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