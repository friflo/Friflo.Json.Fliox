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
    
    public static class PredefinedRoles
    {
        public static readonly  ReadOnlyDictionary<string, Authorizer> Roles;
        
        static PredefinedRoles() {
            var roles = new Dictionary<string, Authorizer>();
            //
            Roles.TryAdd("allow",             new AuthorizeAllow());
            Roles.TryAdd("deny",              new AuthorizeDeny());
            Roles.TryAdd("readOnly",          new AuthorizeReadOnly());
            //
            Roles.TryAdd("read",              new AuthorizeTaskType(TaskType.read));
            Roles.TryAdd("query",             new AuthorizeTaskType(TaskType.query));
            Roles.TryAdd("create",            new AuthorizeTaskType(TaskType.create));
            Roles.TryAdd("update",            new AuthorizeTaskType(TaskType.update));
            Roles.TryAdd("patch",             new AuthorizeTaskType(TaskType.patch));
            Roles.TryAdd("delete",            new AuthorizeTaskType(TaskType.delete));
            Roles.TryAdd("subscribeChanges",  new AuthorizeTaskType(TaskType.subscribeChanges));
            Roles.TryAdd("subscribeMessage",  new AuthorizeTaskType(TaskType.subscribeMessage));
            
            Roles = new ReadOnlyDictionary<string, Authorizer>(roles);
        }
    }
}