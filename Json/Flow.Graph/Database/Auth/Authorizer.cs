// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Auth
{
    public abstract class Authorizer
    {
        public abstract bool Authorize(DatabaseTask task, MessageContext messageContext);
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
    
    public static class PredefinedRoles
    {
        public static readonly  ReadOnlyDictionary<string, Authorizer> Roles;
        
        static PredefinedRoles() {
            var roles = new Dictionary<string, Authorizer>();
            //
            roles.TryAdd("allow",             new AuthorizeAllow());
            roles.TryAdd("deny",              new AuthorizeDeny());
            roles.TryAdd("readOnly",          new AuthorizeReadOnly());
            roles.TryAdd("mutate",            new AuthorizeMutate());
            //
            roles.TryAdd("read",              new AuthorizeTaskType(TaskType.read));
            roles.TryAdd("query",             new AuthorizeTaskType(TaskType.query));
            roles.TryAdd("create",            new AuthorizeTaskType(TaskType.create));
            roles.TryAdd("update",            new AuthorizeTaskType(TaskType.update));
            roles.TryAdd("patch",             new AuthorizeTaskType(TaskType.patch));
            roles.TryAdd("delete",            new AuthorizeTaskType(TaskType.delete));
            roles.TryAdd("subscribeChanges",  new AuthorizeTaskType(TaskType.subscribeChanges));
            roles.TryAdd("subscribeMessage",  new AuthorizeTaskType(TaskType.subscribeMessage));
            
            Roles = new ReadOnlyDictionary<string, Authorizer>(roles);
        }
    }
}