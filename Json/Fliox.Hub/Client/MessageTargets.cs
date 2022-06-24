// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable ClassNeverInstantiated.Global
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary> prototype WIP </summary>
    public struct MessageTargets
    {
        internal IList<UserClient>   userClients;
        
        public MessageTargets (string user) {
            userClients = new List<UserClient> { new UserClient(user) };
        }
        
        public MessageTargets (JsonKey user) {
            userClients = new List<UserClient> { new UserClient(user) };
        }
        
        public MessageTargets (string user, string  client) {
            userClients = new List<UserClient> { new UserClient(user, client) };
        }
        
        public MessageTargets (JsonKey user, JsonKey  client) {
            userClients = new List<UserClient> { new UserClient(user, client) };
        }
        
        public MessageTargets (UserClient userClient) {
            userClients = new List<UserClient> { userClient };
        }
        
        internal void AddClient(UserClient client) {
            if (userClients == null) {
                userClients = new List<UserClient> { client };
                return;
            }
            userClients.Add(client);
        }
        
        internal void AddClients (ICollection<string> users) {
            if (userClients == null) userClients = new List<UserClient>(users.Count);
            foreach (var user in users) {
                userClients.Add(new UserClient(user));
            }
        }
        
        internal void AddClients (ICollection<JsonKey> users) {
            if (userClients == null) userClients = new List<UserClient>(users.Count);
            foreach (var user in users) {
                userClients.Add(new UserClient(user));
            }
        }
        
        internal void AddClients (ICollection<(string, string)> userClients) {
            if (this.userClients == null) this.userClients = new List<UserClient>(userClients.Count);
            foreach (var (user, client) in userClients) {
                this.userClients.Add(new UserClient(user, client));
            }
        }
        
        internal void AddClients (ICollection<UserClient> userClients) {
            if (this.userClients == null) this.userClients = new List<UserClient>(userClients.Count);
            foreach (var element in userClients) {
                this.userClients.Add(element);
            }
        }
    }
    
    public static class MessageTargetExtension
    {
        // --- user
        public static  TTask  TargetUser<TTask> (this TTask messageTask, string  user) where TTask : MessageTask {
            messageTask.Targets.AddClient(new UserClient(user));
            return messageTask;
        }
        
        public static  TTask  TargetUser<TTask> (this TTask messageTask, JsonKey user) where TTask : MessageTask{
            messageTask.Targets.AddClient(new UserClient(user));
            return messageTask;
        }
        
        // --- user client
        public static  TTask  TargetClient<TTask> (this TTask messageTask, string  user, string client) where TTask : MessageTask{
            messageTask.Targets.AddClient(new UserClient(user, client));
            return messageTask;
        }
        
        public static  TTask  TargetClient<TTask> (this TTask messageTask, JsonKey user, JsonKey client) where TTask : MessageTask{
            messageTask.Targets.AddClient(new UserClient(user, client));
            return messageTask;
        }
        
        public static  TTask  TargetClient<TTask> (this TTask messageTask, UserClient client) where TTask : MessageTask {
            messageTask.Targets.AddClient(client);
            return messageTask;
        }
        
        // --- users
        public static  TTask  TargetUsers<TTask> (this TTask messageTask, ICollection<string>  users) where TTask : MessageTask {
            messageTask.Targets.AddClients(users);
            return messageTask;
        }
        public static  TTask  TargetUsers<TTask> (this TTask messageTask, ICollection<JsonKey>  users) where TTask : MessageTask {
            messageTask.Targets.AddClients (users);
            return messageTask;
        }
        
        // --- user clients
        public static  TTask  TargetClients<TTask> (this TTask messageTask, ICollection<(string, string)>  clients) where TTask : MessageTask {
            messageTask.Targets.AddClients(clients);
            return messageTask;
        }
        public static  TTask  TargetClients<TTask> (this TTask messageTask, ICollection<UserClient>  clients) where TTask : MessageTask {
            messageTask.Targets.AddClients (clients);
            return messageTask;
        }
    }
    
    public readonly struct UserClient {
        public  readonly    JsonKey     user;
        public  readonly    JsonKey     client;
        
        public UserClient (string user, string client = null) {
            this.user   = new JsonKey(user);
            this.client = new JsonKey(client);
        }
        
        public UserClient (JsonKey user, JsonKey client = default) {
            this.user   = user;
            this.client = client;
        }
    }
}