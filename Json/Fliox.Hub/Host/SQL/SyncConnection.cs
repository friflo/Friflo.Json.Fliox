// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    /// <summary>
    /// <see cref="ISyncConnection"/> instances are created on demand per <see cref="SyncContext"/>.<br/>
    /// This enables:<br/>
    /// <list type="bullet">
    ///   <item>Execution of multiple databases commands simultaneously</item>
    ///   <item>Execute a set of database commands (tasks) inside a transaction</item>
    /// </list> 
    /// </summary>
    public interface ISyncConnection
    {
        public  TaskExecuteError    Error    { get; }
        public  bool                IsOpen   { get; }
        public  void                Dispose();
    }
    
    public sealed class SyncConnectionError : ISyncConnection {
        public  TaskExecuteError    Error   { get; }
        public  bool                IsOpen  => false;
        public  void                Dispose() { }
        
        public SyncConnectionError(TaskExecuteError error) {
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }
        
        public SyncConnectionError(Exception exception) {
            Error = new TaskExecuteError(exception.Message);
        }
    }
    
    internal sealed class SyncTransaction
    {
        internal readonly   int         taskIndex;
        
        internal SyncTransaction(int taskIndex) {
            this.taskIndex     = taskIndex;
        }
    }
}