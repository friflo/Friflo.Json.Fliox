// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Db.Sync;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Db.Auth.Rights
{
    public class RightTask : Right
    {
        [Fri.Required]  public  List<TaskType>  types;
        
        public  override        RightType       RightType => RightType.task;
        
        private static readonly Authorizer Read             = new AuthorizeTaskType(TaskType.read);
        private static readonly Authorizer Query            = new AuthorizeTaskType(TaskType.query);
        private static readonly Authorizer Create           = new AuthorizeTaskType(TaskType.create);
        private static readonly Authorizer Update           = new AuthorizeTaskType(TaskType.update);
        private static readonly Authorizer Patch            = new AuthorizeTaskType(TaskType.patch);
        private static readonly Authorizer Delete           = new AuthorizeTaskType(TaskType.delete);
        //
        private static readonly Authorizer Message          = new AuthorizeTaskType(TaskType.message);
        private static readonly Authorizer SubscribeChanges = new AuthorizeTaskType(TaskType.subscribeChanges);
        private static readonly Authorizer SubscribeMessage = new AuthorizeTaskType(TaskType.subscribeMessage);
        
        public override Authorizer ToAuthorizer() {
            if (types.Count == 1) {
                return GetAuthorizer(types[0]);
            }
            var list = new List<Authorizer>(types.Count);
            foreach (var task in types) {
                list.Add(GetAuthorizer(task));
            }
            return new AuthorizeAny(list);
        }
        
        private static Authorizer GetAuthorizer(TaskType taskType) {
            switch (taskType) {
                case TaskType.read:                return Read;
                case TaskType.query:               return Query;
                case TaskType.create:              return Create;
                case TaskType.update:              return Update;
                case TaskType.patch:               return Patch;
                case TaskType.delete:              return Delete;
                //
                case TaskType.message:             return Message;
                case TaskType.subscribeChanges:    return SubscribeChanges;
                case TaskType.subscribeMessage:    return SubscribeMessage;
            }
            throw new InvalidOperationException($"unknown authorization taskType: {taskType}");
        }

    }
}