// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Auth
{
    /// <summary>
    /// Authorize a given task.
    /// <br></br>
    /// This <see cref="Authorizer"/> it stored at <see cref="AuthState.Authorizer"/>.
    /// The <see cref="AuthState.Authorizer"/> is set via <see cref="Authenticator.Authenticate"/> for
    /// <see cref="AuthState.Authenticated"/> and for not <see cref="AuthState.Authenticated"/> users.  
    /// </summary>
    public abstract class Authorizer
    {
        public abstract bool Authorize(DatabaseTask task, MessageContext messageContext);
        
        private static readonly  ReadOnlyDictionary<string, Authorizer> Roles;
        
        static Authorizer() {
            var roles = new Dictionary<string, Authorizer>();
            //
            roles.TryAdd("allow",             new AuthorizeAllow());
            roles.TryAdd("deny",              new AuthorizeDeny());
            roles.TryAdd("mutate",            new AuthorizeMutate());
            //
            roles.TryAdd("read",              new AuthorizeTaskType(TaskType.read));
            roles.TryAdd("query",             new AuthorizeTaskType(TaskType.query));
            roles.TryAdd("create",            new AuthorizeTaskType(TaskType.create));
            roles.TryAdd("update",            new AuthorizeTaskType(TaskType.update));
            roles.TryAdd("patch",             new AuthorizeTaskType(TaskType.patch));
            roles.TryAdd("delete",            new AuthorizeTaskType(TaskType.delete));
            roles.TryAdd("message",           new AuthorizeTaskType(TaskType.message));
            //
            roles.TryAdd("subscribeChanges",  new AuthorizeTaskType(TaskType.subscribeChanges));
            roles.TryAdd("subscribeMessage",  new AuthorizeTaskType(TaskType.subscribeMessage));
            
            Roles = new ReadOnlyDictionary<string, Authorizer>(roles);
        }
        
        public static bool GetAuthorizerByRole(string role, out Authorizer authorizer) {
            if (Roles.TryGetValue(role, out authorizer)) {
                return true;
            }
            var compoundCommand = role.Split('/');
            var compoundElements = compoundCommand[0].Split(':');
            var name = compoundElements[0];
            switch (name) {
                case "message":
                    if (compoundElements.Length < 2)
                        return false;
                    var messageName = compoundElements[1];
                    authorizer = new AuthorizeMessage(messageName);
                    return true;
                case "container":
                    if (compoundElements.Length < 2)
                        return false;
                    var container   = compoundElements[1];
                    if (compoundCommand.Length < 2) {
                        authorizer = new AuthorizeContainer(container);
                        return true;
                    }
                    var types       = compoundCommand[1].Split(',');
                    authorizer      = new AuthorizeContainer(container, types);
                    return true;
            }
            return false;
        }
    }
    
    public class AuthorizeAllow : Authorizer {
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            return true;
        }
    }    
    
    public class AuthorizeDeny : Authorizer {
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            return false;
        }
    }
    
    public class AuthorizeAll : Authorizer {
        private readonly ICollection<Authorizer> list;
        
        public AuthorizeAll(ICollection<Authorizer> list) {
            this.list = list;    
        }
        
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            foreach (var item in list) {
                if (!item.Authorize(task, messageContext))
                    return false;
            }
            return true;
        }
    }
    
    public class AuthorizeAny : Authorizer {
        private readonly ICollection<Authorizer> list;
        
        public AuthorizeAny(ICollection<Authorizer> list) {
            this.list = list;    
        }
        
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            foreach (var item in list) {
                if (item.Authorize(task, messageContext))
                    return true;
            }
            return false;
        }
    }
    
    public class AuthorizeTaskType : Authorizer {
        private readonly TaskType type;
        
        public AuthorizeTaskType(TaskType type) {
            this.type = type;    
        }
        
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            return task.TaskType == type;
        }
    }
    
    public class AuthorizeReadOnly : Authorizer {
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            switch (task.TaskType) {
                case TaskType.query:
                case TaskType.read:
                case TaskType.message:
                case TaskType.subscribeChanges:
                case TaskType.subscribeMessage:
                    return true;
            }
            return false;
        }
    }
    
    public class AuthorizeMutate : Authorizer {
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            switch (task.TaskType) {
                case TaskType.create:
                case TaskType.update:
                case TaskType.patch:
                case TaskType.delete:
                    return true;
            }
            return false;
        }
    }
    
    public class AuthorizeMessage : Authorizer {
        private readonly    string  messageName;
        public  override    string  ToString() => messageName;

        public AuthorizeMessage (string message) {
            messageName = message;
        }
        
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            if (task is SendMessage message) {
                return messageName == message.name;
            }
            return false;
        }
    }
    
    public class AuthorizeContainer : Authorizer {
        private readonly    string  container;
        
        private readonly    bool    create;
        private readonly    bool    update;
        private readonly    bool    delete;
        private readonly    bool    patch;
        
        private readonly    bool    read;
        private readonly    bool    query;

        public  override    string  ToString() => container;
        
        public AuthorizeContainer (string container) {
            this.container = container;
            create  = true;
            update  = true;
            delete  = true;
            patch   = true;
            read    = true;
            query   = true;
        }

        public AuthorizeContainer (string container, ICollection<string> types) {
            this.container = container;
            create  = types.Contains("create");
            update  = types.Contains("update");
            delete  = types.Contains("delete");
            patch   = types.Contains("patch");
            
            read    = types.Contains("read");
            query   = types.Contains("query");
        }
        
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            switch (task.TaskType) {
                case TaskType.create:   return create && ((CreateEntities)  task).container == container;
                case TaskType.update:   return update && ((UpdateEntities)  task).container == container;
                case TaskType.delete:   return delete && ((DeleteEntities)  task).container == container;
                case TaskType.patch:    return patch  && ((PatchEntities)   task).container == container;
                
                case TaskType.read:     return read   && ((ReadEntitiesList)task).container == container;
                case TaskType.query:    return query  && ((QueryEntities)   task).container == container;
            }
            return false;
        }
    }
    
}