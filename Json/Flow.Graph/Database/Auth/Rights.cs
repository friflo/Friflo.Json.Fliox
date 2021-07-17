// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable CollectionNeverUpdated.Global
namespace Friflo.Json.Flow.Database.Auth
{
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(RightAllow),              Discriminant = "allow")]
    [Fri.Polymorph(typeof(RightTask),               Discriminant = "task")]
    [Fri.Polymorph(typeof(RightMessage),            Discriminant = "message")]
    [Fri.Polymorph(typeof(RightSubscribeMessage),   Discriminant = "subscribeMessage")]
    [Fri.Polymorph(typeof(RightDatabase),           Discriminant = "database")]
    [Fri.Polymorph(typeof(RightPredicate),          Discriminant = "predicate")]
    public abstract class Right {
        public          string          description;
        public abstract RightType       RightType { get; }

        public abstract Authorizer      ToAuthorizer();
    }
    
    public class RightAllow : Right
    {
        public          bool                    grant;
        public override RightType               RightType => RightType.allow;

        public override string                  ToString() => grant.ToString();

        private  static readonly Authorizer Allow = new AuthorizeAllow();
        internal static readonly Authorizer Deny  = new AuthorizeDeny();
        
        public override Authorizer ToAuthorizer() {
            return grant ? Allow : Deny;
        }
    }
    
    public class RightTask : Right
    {
        public          List<TaskType>          types;
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
    
    public class RightMessage : Right
    {
        public          List<string>            names;
        public override RightType               RightType => RightType.message;
        
        public override Authorizer ToAuthorizer() {
            if (names.Count == 1) {
                return new AuthorizeMessage(names[0]);
            }
            var list = new List<Authorizer>(names.Count);
            foreach (var message in names) {
                list.Add(new AuthorizeMessage(message));
            }
            return new AuthorizeAny(list);
        }
    }
    
    public class RightSubscribeMessage : Right
    {
        public          List<string>            names;
        public override RightType               RightType => RightType.subscribeMessage;
        
        public override Authorizer ToAuthorizer() {
            if (names.Count == 1) {
                return new AuthorizeSubscribeMessage(names[0]);
            }
            var list = new List<Authorizer>(names.Count);
            foreach (var message in names) {
                list.Add(new AuthorizeSubscribeMessage(message));
            }
            return new AuthorizeAny(list);
        }
    }
    
    public class RightDatabase : Right
    {
        public          Dictionary<string, ContainerAccess> containers;
        public override RightType                           RightType => RightType.access;
        
        public override Authorizer ToAuthorizer() {
            var list = new List<Authorizer>(containers.Count);
            foreach (var pair in containers) {
                var name        = pair.Key;
                var container   = pair.Value;
                var access      = container.operations;
                if (access != null && access.Count > 0) {
                    list.Add(new AuthorizeContainer(name, access));
                }
                var subscribeChanges   = container.subscribeChanges;
                if (subscribeChanges != null && subscribeChanges.Count > 0) {
                    list.Add(new AuthorizeSubscribeChanges(name, subscribeChanges));
                }
            }
            return new AuthorizeAny(list);
        }
    }
    
    public class ContainerAccess
    {
        public          List<AccessType>        operations;
        public          List<Change>            subscribeChanges;
    }
    
    public class RightPredicate : Right
    {
        public          List<string>            names;
        public override RightType               RightType => RightType.predicate;
        
        public override Authorizer ToAuthorizer() {
            throw new NotImplementedException();
        }
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
        full
    }
    
    public enum RightType {
        allow,
        tasks,
        message,
        subscribeMessage,
        access,
        predicate
    }
}