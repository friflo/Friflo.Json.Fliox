// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeContainer : IAuthorizer {
        private  readonly   AuthorizeDatabase authorizeDatabase;
        
        private  readonly   string  container;
        
        private  readonly   bool    create;
        private  readonly   bool    upsert;
        private  readonly   bool    delete;
        private  readonly   bool    deleteAll;
        private  readonly   bool    patch;
        //
        private  readonly   bool    read;
        private  readonly   bool    query;
        private  readonly   bool    aggregate;

        public   override   string  ToString() => $"database: {authorizeDatabase.dbLabel}, container: {container}";
        
        public AuthorizeContainer (string container, ICollection<OperationType> types, string database)
        {
            authorizeDatabase   = new AuthorizeDatabase(database);
            this.container      = container;
            SetRoles(types, ref create, ref upsert, ref delete, ref deleteAll, ref patch, ref read, ref query, ref aggregate);
        }
        
        private static void SetRoles (ICollection<OperationType> types,
                ref bool create, ref bool upsert, ref bool delete, ref bool deleteAll, ref bool patch,
                ref bool read,   ref bool query, ref bool aggregate)
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
                    case OperationType.aggregate:   aggregate   = true;   break;
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
        
        public void AddAuthorizedDatabases(HashSet<AuthorizeDatabase> databases) => databases.Add(authorizeDatabase);

        public bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            if (!authorizeDatabase.Authorize(executeContext))
                return false;
            switch (task.TaskType) {
                case TaskType.create:       return create       && ((CreateEntities)    task).container == container;
                case TaskType.upsert:       return upsert       && ((UpsertEntities)    task).container == container;
                case TaskType.delete:
                    var deleteEntities = (DeleteEntities)  task;
                    return deleteEntities.Authorize(container, delete, deleteAll);
                case TaskType.patch:        return patch        && ((PatchEntities)     task).container == container;
                //
                case TaskType.read:         return read         && ((ReadEntities)      task).container == container;
                case TaskType.query:        return query        && ((QueryEntities)     task).container == container;
                case TaskType.closeCursors: return query        && ((CloseCursors)      task).container == container;
                case TaskType.aggregate:    return aggregate    && ((AggregateEntities) task).container == container;
            }
            return false;
        }
    }
}