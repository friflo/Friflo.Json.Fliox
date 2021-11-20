// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Auth.Rights;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Auth
{
    /// <summary>
    /// Authorize a given task.
    /// <br></br>
    /// This <see cref="Authorizer"/> it stored at <see cref="AuthState.authorizer"/>.
    /// The <see cref="AuthState.authorizer"/> is set via <see cref="Authenticator.Authenticate"/> for
    /// <see cref="AuthState.authenticated"/> and for not <see cref="AuthState.authenticated"/> users.  
    /// </summary>
    public abstract class Authorizer
    {
        public abstract bool Authorize(SyncRequestTask task, MessageContext messageContext);
    }
    
    public abstract class AuthorizerDatabase : Authorizer
    {
        private    readonly     string  database;
        private    readonly     bool    isPrefix;
        protected  readonly     string  dbLabel;
        
        protected AuthorizerDatabase () {
            isPrefix    = true;
            database    = "";
            dbLabel     = "*";
        }
        protected AuthorizerDatabase (string database) {
            if (database == null) {
                dbLabel = EntityDatabase.DefaultDb;
                return;
            }
            dbLabel     = database;
            isPrefix    = database.EndsWith("*");
            if (isPrefix) {
                this.database = database.Substring(0, database.Length - 1);
            } else {
                this.database = database;
            }
        }
        
        protected bool AuthorizeDatabase(MessageContext messageContext) {
            var db = messageContext.DatabaseName;
            if (isPrefix) {
                if (db != null) 
                    return db.StartsWith(database);
                return database.Length == 0;
            }
            return db == database;
        }
    }

    
    public sealed class AuthorizeAllow : AuthorizerDatabase {
        public override string ToString() => $"database: {dbLabel}";

        public AuthorizeAllow () { }
        public AuthorizeAllow (string database) : base (database) { }
        
        public override bool Authorize(SyncRequestTask task, MessageContext messageContext) {
            return AuthorizeDatabase(messageContext);
        }
    }    
    
    public sealed class AuthorizeDeny : Authorizer {
        public override bool Authorize(SyncRequestTask task, MessageContext messageContext) {
            return false;
        }
    }
    
    public sealed class AuthorizeAll : Authorizer {
        private readonly    ICollection<Authorizer>     list;
        
        public AuthorizeAll(ICollection<Authorizer> list) {
            this.list = list;    
        }
        
        public override bool Authorize(SyncRequestTask task, MessageContext messageContext) {
            foreach (var item in list) {
                if (!item.Authorize(task, messageContext))
                    return false;
            }
            return true;
        }
    }
    
    public sealed class AuthorizeAny : Authorizer {
        private readonly    ICollection<Authorizer>     list;
        
        public AuthorizeAny(ICollection<Authorizer> list) {
            this.list = list;    
        }
        
        public override bool Authorize(SyncRequestTask task, MessageContext messageContext) {
            foreach (var item in list) {
                if (item.Authorize(task, messageContext))
                    return true;
            }
            return false;
        }
    }
    
    public sealed class AuthorizeTaskType : AuthorizerDatabase {
        private  readonly   TaskType    type;
        
        public   override   string      ToString() => $"database: {dbLabel}, type: {type.ToString()}";

        public AuthorizeTaskType(TaskType type, string database) : base (database) {
            this.type       = type;    
        }
        
        public override bool Authorize(SyncRequestTask task, MessageContext messageContext) {
            if (!AuthorizeDatabase(messageContext))
                return false;
            return task.TaskType == type;
        }
    }
    
    public sealed class AuthorizeMessage : AuthorizerDatabase {
        private  readonly   string      messageName;
        private  readonly   bool        prefix;
        private  readonly   string      messageLabel;
        public   override   string      ToString() => $"database: {dbLabel}, message: {messageLabel}";

        public AuthorizeMessage (string message, string database) : base (database) {
            messageLabel = message;
            if (message.EndsWith("*")) {
                prefix = true;
                messageName = message.Substring(0, message.Length - 1);
                return;
            }
            messageName = message;
        }
        
        public override bool Authorize(SyncRequestTask task, MessageContext messageContext) {
            if (!AuthorizeDatabase(messageContext))
                return false;
            if (!(task is SyncMessageTask messageTask))
                return false;
            if (prefix) {
                return messageTask.name.StartsWith(messageName);
            }
            return messageTask.name == messageName;
        }
    }
    
    public sealed class AuthorizeSubscribeMessage : AuthorizerDatabase {
        private  readonly   string      messageName;
        private  readonly   bool        prefix;
        private  readonly   string      messageLabel;
        public   override   string      ToString() => $"database: {dbLabel}, message: {messageLabel}";

        public AuthorizeSubscribeMessage (string message, string database) : base (database) {
            messageLabel = message;
            if (message.EndsWith("*")) {
                prefix = true;
                messageName = message.Substring(0, message.Length - 1);
                return;
            }
            messageName = message;
        }
        
        public override bool Authorize(SyncRequestTask task, MessageContext messageContext) {
            if (!AuthorizeDatabase(messageContext))
                return false;
            if (!(task is SubscribeMessage subscribe))
                return false;
            if (prefix) {
                return subscribe.name.StartsWith(messageName);
            }
            return subscribe.name == messageName;
        }
    }
    
    public sealed class AuthorizeContainer : AuthorizerDatabase {
        private  readonly   string  container;
        
        private  readonly   bool    create;
        private  readonly   bool    upsert;
        private  readonly   bool    delete;
        private  readonly   bool    deleteAll;
        private  readonly   bool    patch;
        //
        private  readonly   bool    read;
        private  readonly   bool    query;

        public   override   string  ToString() => $"database: {dbLabel}, container: {container}";
        
        public AuthorizeContainer (string container, ICollection<OperationType> types, string database)
            : base (database)
        {
            this.container  = container;
            SetRoles(types, ref create, ref upsert, ref delete, ref deleteAll, ref patch, ref read, ref query);
        }
        
        private static void SetRoles (ICollection<OperationType> types,
                ref bool create, ref bool upsert, ref bool delete, ref bool deleteAll, ref bool patch,
                ref bool read,   ref bool query)
        {
            foreach (var type in types) {
                switch (type) {
                    case OperationType.create:      create      = true;   break;
                    case OperationType.upsert:      upsert      = true;   break;
                    case OperationType.delete:      delete      = true;   break;
                    case OperationType.deleteAll:   deleteAll   = true;   break;
                    case OperationType.patch:       patch       = true;   break;
                    //
                    case OperationType.read:        read        = true;   break;
                    case OperationType.query:       query       = true;   break;
                    case OperationType.mutate:
                        create  = true; upsert  = true; delete  = true; patch   = true;
                        break;
                    case OperationType.full:
                        create  = true; upsert  = true; delete  = true; patch   = true;
                        read    = true; query   = true;
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid container role: {type}");
                }
            }
        }
        
        public override bool Authorize(SyncRequestTask task, MessageContext messageContext) {
            if (!AuthorizeDatabase(messageContext))
                return false;
            switch (task.TaskType) {
                case TaskType.create:       return create       && ((CreateEntities)  task).container == container;
                case TaskType.upsert:       return upsert       && ((UpsertEntities)  task).container == container;
                case TaskType.delete:
                    var deleteEntities = (DeleteEntities)  task;
                    return deleteEntities.Authorize(container, delete, deleteAll);
                case TaskType.patch:        return patch        && ((PatchEntities) task).container == container;
                //
                case TaskType.read:         return read         && ((ReadEntities)  task).container == container;
                case TaskType.query:        return query        && ((QueryEntities) task).container == container;
            }
            return false;
        }
    }
    
    public sealed class AuthorizeSubscribeChanges : AuthorizerDatabase {
        private  readonly   string  container;
        
        private  readonly   bool    create;
        private  readonly   bool    upsert;
        private  readonly   bool    delete;
        private  readonly   bool    patch;
        
        public   override   string  ToString() => $"database: {dbLabel}, container: {container}";
        
        public AuthorizeSubscribeChanges (string container, ICollection<Change> changes, string database)
            : base (database)
        {
            this.container = container;
            foreach (var change in changes) {
                switch (change) {
                    case Change.create: create = true; break;
                    case Change.upsert: upsert = true; break;
                    case Change.delete: delete = true; break;
                    case Change.patch:  patch  = true; break;
                }
            }
        }
        
        public override bool Authorize(SyncRequestTask task, MessageContext messageContext) {
            if (!(task is SubscribeChanges subscribe))
                return false;
            if (subscribe.container != container)
                return false;
            var authorize = true;
            foreach (var change in subscribe.changes) {
                switch (change) {
                    case Change.create:     authorize &= create;    break;
                    case Change.upsert:     authorize &= upsert;    break;
                    case Change.delete:     authorize &= delete;    break;
                    case Change.patch:      authorize &= patch;     break;
                }
            }
            return authorize;
        }
    }
    
    public delegate bool AuthPredicate (SyncRequestTask task, IPool pool);
    
    public sealed class AuthorizePredicate : Authorizer {
        private readonly string         name;
        private readonly AuthPredicate  predicate;
        public  override string         ToString() => name;

        public AuthorizePredicate (string name, AuthPredicate predicate) {
            this.name       = name;
            this.predicate  = predicate;    
        }
            
        public override bool Authorize(SyncRequestTask task, MessageContext messageContext) {
            return predicate(task, messageContext.pool);
        }
    }
}