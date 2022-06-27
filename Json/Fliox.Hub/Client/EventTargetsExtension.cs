// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.Hub.Client
{
    public static class EventTargetsExtension
    {
        // --- user
        /// <summary> Send the <see cref="message"/> as an event only to the given <see cref="user"/> </summary>
        public static  TTask  EventTargetUser<TTask> (this TTask message, string  user) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddUser(user);
            return message;
        }
        
        /// <summary> Send the <see cref="message"/> as an event only to the given <see cref="user"/> </summary>
        public static  TTask  EventTargetUser<TTask> (this TTask message, JsonKey user) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddUser(user);
            return message;
        }
        
        // --- client
        /// <summary> Send the <see cref="message"/> as an event only to the given <see cref="client"/> </summary>
        public static  TTask  EventTargetClient<TTask> (this TTask message, string client) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddClient(client);
            return message;
        }
        
        /// <summary> Send the <see cref="message"/> as an event only to the given <see cref="client"/> </summary>
        public static  TTask  EventTargetClient<TTask> (this TTask message, JsonKey client) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddClient(client);
            return message;
        }
        
        // --- users
        /// <summary> Send the <see cref="message"/> as an event only to the given <see cref="users"/> </summary>
        public static  TTask  EventTargetUsers<TTask> (this TTask message, ICollection<string> users) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddUsers(users);
            return message;
        }
        
        /// <summary> Send the <see cref="message"/> as an event only to the given <see cref="users"/> </summary>
        public static  TTask  EventTargetUsers<TTask> (this TTask message, ICollection<JsonKey> users) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddUsers (users);
            return message;
        }
        
        // --- clients
        /// <summary> Send the <see cref="message"/> as an event only to the given <see cref="clients"/> </summary>
        public static  TTask  EventTargetClients<TTask> (this TTask message, ICollection<string>  clients) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddClients(clients);
            return message;
        }
        
        /// <summary> Send the <see cref="message"/> as an event only to the given <see cref="clients"/> </summary>
        public static  TTask  EventTargetClients<TTask> (this TTask message, ICollection<JsonKey>  clients) where TTask : MessageTask {
            var targets = message.GetOrCreateTargets();
            targets.AddClients (clients);
            return message;
        }
    }
}