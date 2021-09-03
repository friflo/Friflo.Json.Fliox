// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Db.Auth.Rights;
using Friflo.Json.Fliox.Db.Database;
using Friflo.Json.Fliox.Db.Sync;

namespace Friflo.Json.Fliox.Db.Auth
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
        private readonly    ICollection<Authorizer>     list;
        
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
        private readonly    ICollection<Authorizer>     list;
        
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
        private  readonly   TaskType    type;
        public   override   string      ToString() => type.ToString();

        public AuthorizeTaskType(TaskType type) {
            this.type = type;    
        }
        
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            return task.TaskType == type;
        }
    }
    
    public class AuthorizeMessage : Authorizer {
        private  readonly   string      messageName;
        private  readonly   bool        prefix;
        public   override   string      ToString() => prefix ? $"{messageName}*" : messageName;

        public AuthorizeMessage (string message) {
            if (message.EndsWith("*")) {
                prefix = true;
                messageName = message.Substring(0, message.Length - 1);
                return;
            }
            messageName = message;
        }
        
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            if (!(task is SendMessage message))
                return false;
            if (prefix) {
                return message.name.StartsWith(messageName);
            }
            return message.name == messageName;
        }
    }
    
    public class AuthorizeSubscribeMessage : Authorizer {
        private  readonly   string      messageName;
        private  readonly   bool        prefix;
        public   override   string      ToString() => prefix ? $"{messageName}*" : messageName;

        public AuthorizeSubscribeMessage (string message) {
            if (message.EndsWith("*")) {
                prefix = true;
                messageName = message.Substring(0, message.Length - 1);
                return;
            }
            messageName = message;
        }
        
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            if (!(task is SubscribeMessage subscribe))
                return false;
            if (prefix) {
                return subscribe.name.StartsWith(messageName);
            }
            return subscribe.name == messageName;
        }
    }
    
    public class AuthorizeContainer : Authorizer {
        private  readonly   string  container;
        
        private  readonly   bool    create;
        private  readonly   bool    update;
        private  readonly   bool    delete;
        private  readonly   bool    patch;
        //
        private  readonly   bool    read;
        private  readonly   bool    query;

        public   override   string  ToString() => container;
        
        public AuthorizeContainer (string container, ICollection<OperationType> types) {
            this.container = container;
            SetRoles(types, ref create, ref update, ref delete, ref patch, ref read, ref query);
        }
        
        private static void SetRoles (ICollection<OperationType> types,
                ref bool create, ref bool update, ref bool delete, ref bool patch,
                ref bool read,   ref bool query)
        {
            foreach (var type in types) {
                switch (type) {
                    case OperationType.create:  create  = true;   break;
                    case OperationType.update:  update  = true;   break;
                    case OperationType.delete:  delete  = true;   break;
                    case OperationType.patch:   patch   = true;   break;
                    //
                    case OperationType.read:    read    = true;   break;
                    case OperationType.query:   query   = true;   break;
                    case OperationType.mutate:
                        create  = true; update  = true; delete  = true; patch   = true;
                        break;
                    case OperationType.full:
                        create  = true; update  = true; delete  = true; patch   = true;
                        read    = true; query   = true;
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid container role: {type}");
                }
            }
        }
        
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            switch (task.TaskType) {
                case TaskType.create:   return create && ((CreateEntities)  task).container == container;
                case TaskType.update:   return update && ((UpdateEntities)  task).container == container;
                case TaskType.delete:   return delete && ((DeleteEntities)  task).container == container;
                case TaskType.patch:    return patch  && ((PatchEntities)   task).container == container;
                //
                case TaskType.read:     return read   && ((ReadEntitiesList)task).container == container;
                case TaskType.query:    return query  && ((QueryEntities)   task).container == container;
            }
            return false;
        }
    }
    
    public class AuthorizeSubscribeChanges : Authorizer {
        private  readonly   string  container;
        
        private  readonly   bool    create;
        private  readonly   bool    update;
        private  readonly   bool    delete;
        private  readonly   bool    patch;
        
        public   override   string  ToString() => container;
        
        public AuthorizeSubscribeChanges (string container, ICollection<Change> changes) {
            this.container = container;
            foreach (var change in changes) {
                switch (change) {
                    case Change.create: create = true; break;
                    case Change.update: update = true; break;
                    case Change.delete: delete = true; break;
                    case Change.patch:  patch  = true; break;
                }
            }
        }
        
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            if (!(task is SubscribeChanges subscribe))
                return false;
            if (subscribe.container != container)
                return false;
            var authorize = true;
            foreach (var change in subscribe.changes) {
                switch (change) {
                    case Change.create:     authorize &= create;    break;
                    case Change.update:     authorize &= update;    break;
                    case Change.delete:     authorize &= delete;    break;
                    case Change.patch:      authorize &= patch;     break;
                }
            }
            return authorize;
        }
    }
    
    public delegate bool AuthPredicate (DatabaseTask task, MessageContext messageContext);
    
    public class AuthorizePredicate : Authorizer {
        private readonly string         name;
        private readonly AuthPredicate  predicate;
        public  override string         ToString() => name;

        public AuthorizePredicate (string name, AuthPredicate predicate) {
            this.name       = name;
            this.predicate  = predicate;    
        }
            
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            return predicate(task, messageContext);
        }
    }
    
}