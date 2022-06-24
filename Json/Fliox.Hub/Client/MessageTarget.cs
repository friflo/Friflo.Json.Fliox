// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable ClassNeverInstantiated.Global
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary> prototype WIP </summary>
    public struct MessageTarget
    {
        internal IList<UserClient>   userClients;
        
        public MessageTarget (string user) {
            userClients = new List<UserClient> { new UserClient(user) };
        }
        
        public MessageTarget (JsonKey user) {
            userClients = new List<UserClient> { new UserClient(user) };
        }
        
        public MessageTarget (string user, string  client) {
            userClients = new List<UserClient> { new UserClient(user, client) };
        }
        
        public MessageTarget (JsonKey user, JsonKey  client) {
            userClients = new List<UserClient> { new UserClient(user, client) };
        }
        
        public MessageTarget (UserClient userClient) {
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
        public static  MessageTask  ToUser (this MessageTask messageTask, string  user) {
            messageTask.Target.AddClient(new UserClient(user));
            return messageTask;
        }
        public static  MessageTask  ToUser (this MessageTask messageTask, JsonKey user) {
            messageTask.Target.AddClient(new UserClient(user));
            return messageTask;
        }
        
        // --- user client
        public static  MessageTask  ToClient (this MessageTask messageTask, string  user, string client) {
            messageTask.Target.AddClient(new UserClient(user, client));
            return messageTask;
        }
        
        public static  MessageTask  ToClient (this MessageTask messageTask, JsonKey user, JsonKey client) {
            messageTask.Target.AddClient(new UserClient(user, client));
            return messageTask;
        }
        
        public static  MessageTask  ToClient (this MessageTask messageTask, UserClient client) {
            messageTask.Target.AddClient(client);
            return messageTask;
        }
        
        // --- users
        public static  MessageTask  ToUsers (this MessageTask messageTask, ICollection<string>  users) {
            messageTask.Target.AddClients(users);
            return messageTask;
        }
        public static  MessageTask  ToUsers (this MessageTask messageTask, ICollection<JsonKey>  users) {
            messageTask.Target.AddClients (users);
            return messageTask;
        }
        
        // --- user clients
        public static  MessageTask  ToClients (this MessageTask messageTask, ICollection<(string, string)>  clients) {
            messageTask.Target.AddClients(clients);
            return messageTask;
        }
        public static  MessageTask  ToClients (this MessageTask messageTask, ICollection<UserClient>  clients) {
            messageTask.Target.AddClients (clients);
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