// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Auth
{
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(RightAllow),      Discriminant = "allow")]
    [Fri.Polymorph(typeof(RightTasks),      Discriminant = "tasks")]
    [Fri.Polymorph(typeof(RightMessages),   Discriminant = "messages")]
    [Fri.Polymorph(typeof(RightContainers), Discriminant = "containers")]
    public abstract class Right {
        
        public abstract RightType       RightType { get; }

        public abstract Authorizer      ToAuthorizer();
    }
    
    public class RightAllow : Right
    {
        public          bool                    allow;
        public override RightType               RightType => RightType.allow;

        public override string                  ToString() => allow.ToString();

        private static readonly Authorizer Allow = new AuthorizeAllow();
        private static readonly Authorizer Deny  = new AuthorizeDeny();
        
        public override Authorizer ToAuthorizer() {
            return allow ? Allow : Deny;
        }
    }
    
    public class RightTasks : Right
    {
        public          List<TaskType>          tasks;
        public override RightType               RightType => RightType.tasks;
        
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
            if (tasks.Count == 1) {
                return GetAuthorizer(tasks[0]);
            }
            var list = new List<Authorizer>(tasks.Count);
            foreach (var task in tasks) {
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
    
    public class RightMessages : Right
    {
        public          List<string>            messages;
        public override RightType               RightType => RightType.messages;
        
        public override Authorizer ToAuthorizer() {
            if (messages.Count == 1) {
                return new AuthorizeMessage(messages[0]);
            }
            var list = new List<Authorizer>(messages.Count);
            foreach (var message in messages) {
                list.Add(new AuthorizeMessage(message));
            }
            return new AuthorizeAny(list);
        }
    }
    
    public class RightContainers : Right
    {
        public          Dictionary<string, ContainerAccess> containers;
        public override RightType                           RightType => RightType.containers;
        
        public override Authorizer ToAuthorizer() {
            var list = new List<Authorizer>(containers.Count);
            foreach (var (name, container) in containers) {
                var access = container.access;
                if (access == null || access.Count == 0) {
                    list.Add(new AuthorizeContainer(name));
                } else {
                    list.Add(new AuthorizeContainer(name, access));
                }
            }
            return new AuthorizeAny(list);
        }
    }
    
    public class ContainerAccess
    {
        public          List<AccessType>            access;
    }
    
    

    // ReSharper disable InconsistentNaming
    public enum AccessType {
        create,
        update,
        delete,
        patch, 
        read,  
        query, 
        mutate,
    }
    
    public enum RightType {
        allow,
        tasks,
        messages,
        containers,
    }
}