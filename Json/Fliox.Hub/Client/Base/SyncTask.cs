// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Base class of all tasks create via methods of <see cref="FlioxClient"/> and <see cref="EntitySet{TKey,T}"/>
    /// </summary>
    public abstract class SyncTask
    {
        internal  readonly  Set       taskSet;
        
        internal  abstract  TaskType        TaskType { get; }
        internal  abstract  SyncRequestTask CreateRequestTask(in CreateTaskContext context);
        
                                    internal            string              taskName;
                                    internal            string              GetLabel() => taskName ?? Details;
        [DebuggerBrowsable(Never)]  public    abstract  string              Details { get; }
        [DebuggerBrowsable(Never)]  internal  abstract  TaskState           State   { get; }
        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// A handler method assigned to <see cref="OnSync"/> is called after executing a <see cref="SyncTask"/> with
        /// <see cref="FlioxClient.SyncTasks"/>. <br/>
        /// This is an alternative way to process a <see cref="SyncTask"/> result to the common task processing shown below.
        /// </summary>
        /// <remarks>
        /// It is intended to be used in scenarios where a set of tasks are not created in a single function block
        /// like in the example below. <br/>
        /// This is typical for game applications where <see cref="FlioxClient.SyncTasks"/> is called by the game loop
        /// for every frame to batch multiple <see cref="SyncTask"/>'s created on various places. <br/>
        /// <code>
        /// // Common task processing
        /// var articles = client.articles.QueryAll();
        /// var orders   = client.orders.QueryAll();
        /// await client.SyncTasks(); 
        /// foreach (var article in articles) { ... }
        /// foreach (var order in orders) { ... }
        /// </code>
        /// </remarks>
        [DebuggerBrowsable(Never)]  public              Action<TaskError>   OnSync;
        
                                    public    override  string              ToString()  => GetLabel();

        internal SyncTask() { }
                                    
        internal SyncTask(Set set) {
            taskSet = set; 
        }
                                    
        /// <summary>
        /// Is true in case task execution was successful. Otherwise false. If false <see cref="Error"/> property is set. 
        /// </summary>
        /// <exception cref="TaskNotSyncedException"></exception>
        public              bool        Success { get {
            if (State.IsExecuted())
                return !State.Error.HasErrors;
            throw new TaskNotSyncedException($"SyncTask.Success requires SyncTasks(). {GetLabel()}");
        }}

        /// <summary>The error caused the task failing. Return null if task was successful - <see cref="Success"/> == true</summary>
        public              TaskError   Error { get {
            if (State.IsExecuted())
                return State.Error.TaskError;
            throw new TaskNotSyncedException($"SyncTask.Error requires SyncTasks(). {GetLabel()}");
        } }
        
        protected internal virtual void Reuse() { }

        internal bool IsOk(string method, out Exception e) {
            if (State.IsExecuted()) {
                if (!State.Error.HasErrors) {
                    e = null;
                    return true;
                }
                e = new TaskResultException(State.Error.TaskError);
                return false;
            }
            e = new TaskNotSyncedException($"{method} requires SyncTasks(). {GetLabel()}");
            return false;
        }
        
        internal Exception AlreadySyncedError() {
            return new TaskAlreadySyncedException($"Task already executed. {GetLabel()}");
        }
    }
    
    internal readonly struct CreateTaskContext
    {
        internal  readonly  ObjectMapper    mapper;
        internal CreateTaskContext(ObjectMapper mapper) {
            this.mapper = mapper;
        }
    }
    
    public static class SyncTaskExtension
    {
        /// <summary>
        /// An arbitrary name which can be assigned to a task. Typically the name of the variable the task is assigned to.
        /// The <paramref name="name"/> is used to simplify finding a <see cref="SyncTask"/> in the source code while debugging.
        /// It also simplifies finding a <see cref="TaskError"/> by its <see cref="TaskError.Message"/>
        /// or a <see cref="TaskResultException"/> by its <see cref="Exception.Message"/>.
        /// The library itself doesn't use the <paramref name="name"/> internally - its purpose is only to enhance debugging
        /// or post-mortem debugging of application code.
        /// </summary>
        public static T TaskName<T> (this T task, string name) where T : SyncTask {
            task.taskName = name;
            return task;
        }
    }
}