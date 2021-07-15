// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        private readonly TaskType   type;
        public  override string     ToString() => type.ToString();

        public AuthorizeTaskType(TaskType type) {
            this.type = type;    
        }
        
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            return task.TaskType == type;
        }
    }
    
    public class AuthorizeMessage : Authorizer {
        private readonly    string  messageName;
        private readonly    bool    prefix;
        public  override    string  ToString() => prefix ? $"{messageName}*" : messageName;

        public AuthorizeMessage (string message) {
            if (message.EndsWith("*")) {
                prefix = true;
                messageName = message.Substring(0, message.Length - 1);
                return;
            }
            messageName = message;
        }
        
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            if (task is SendMessage message) {
                if (prefix) {
                    return message.name.StartsWith(messageName);
                }
                return message.name == messageName;
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
        //
        private readonly    bool    read;
        private readonly    bool    query;

        public  override    string  ToString() => container;
        
        public AuthorizeContainer (string container, ICollection<AccessType> types) {
            this.container = container;
            SetRoles(types, ref create, ref update, ref delete, ref patch, ref read, ref query);
        }
        
        private static void SetRoles (ICollection<AccessType> types,
                ref bool create, ref bool update, ref bool delete, ref bool patch,
                ref bool read,   ref bool query) {
            foreach (var type in types) {
                switch (type) {
                    case AccessType.create:  create  = true;   break;
                    case AccessType.update:  update  = true;   break;
                    case AccessType.delete:  delete  = true;   break;
                    case AccessType.patch:   patch   = true;   break;
                    //
                    case AccessType.read:    read    = true;   break;
                    case AccessType.query:   query   = true;   break;
                    case AccessType.mutate:
                        create  = true; update  = true; delete  = true; patch   = true;
                        break;
                    case AccessType.full:
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