// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public sealed class SubscribeChangesTask<T> : SyncTask where T : class
    {
        [DebuggerBrowsable(Never)]
        internal            TaskState           state;
        internal            List<EntityChange>  changes;
        internal            FilterOperation     filter;
        private             string              filterLinq; // use as string identifier of a filter
        [DebuggerBrowsable(Never)]
        private readonly    Set<T>              setBase;
            
        internal override   TaskState           State   => state;
        public   override   string              Details => $"SubscribeChangesTask<{typeof(T).Name}> (filter: {filterLinq})";
        internal override   TaskType            TaskType=> TaskType.subscribeChanges;
        
        internal  SubscribeChangesTask(Set<T> set) : base(set) {
            setBase    = set;
        }
            
        internal void Set(IEnumerable<EntityChange> changes, FilterOperation filter) {
            this.changes    = changes != null ? changes.ToList() : new List<EntityChange>();
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return setBase.SubscribeChanges(this, context);
        }
    }
    
    /// <summary>Filter type used to specify the type of an entity change.</summary>
    // ReSharper disable InconsistentNaming
    [Flags]
    public enum Change
    {
        /// <summary>Shortcut to unsubscribe from all entity change types.</summary>
        None    = 0,
        /// <summary>Shortcut to subscribe to all types of entity changes.</summary>
        /// <remarks>
        /// These ase <see cref="Change.create"/>, <see cref="Change.upsert"/>, <see cref="Change.merge"/> and <see cref="Change.delete"/>
        /// </remarks>
        All     = 1 | 2 | 4 | 8,
        
        /// <summary>filter change events of created entities.</summary>
        create  = 1,
        /// <summary>filter change events of upserted entities.</summary>
        upsert  = 2,
        /// <summary>filter change events of entity patches.</summary>
        merge   = 4,
        /// <summary>filter change events of deleted entities.</summary>
        delete  = 8
    }
    
    internal static class ChangeExtension
    {
        internal static IReadOnlyList<EntityChange> ChangeToList(this Change change) {
            var list = new List<EntityChange>(4);
            if ((change & Change.create) != 0) list.Add(EntityChange.create);
            if ((change & Change.upsert) != 0) list.Add(EntityChange.upsert);
            if ((change & Change.delete) != 0) list.Add(EntityChange.delete);
            if ((change & Change.merge)  != 0) list.Add(EntityChange.merge);
            return list;
        }
    }
    
    public sealed class SubscribeMessageTask : SyncTask
    {
        private  readonly   string      name;
        private  readonly   bool?       remove;
        [DebuggerBrowsable(Never)]
        internal            TaskState   state;
            
        internal override   TaskState   State   => state;
        public   override   string      Details => $"SubscribeMessageTask (name: {name})";
        internal override   TaskType    TaskType=> TaskType.subscribeMessage;
        

        internal SubscribeMessageTask(string name, bool? remove) {
            this.name   = name;
            this.remove = remove;
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return new SubscribeMessage{ name = name, remove = remove, intern = new SyncTaskIntern(this) };
        }
    }
}