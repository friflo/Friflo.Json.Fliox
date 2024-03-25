// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

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
        public  void                ClearPool();
    }
    
    public class DefaultSyncConnection : ISyncConnection
    {
        public  TaskExecuteError    Error    => null;
        public  bool                IsOpen   => true;
        public  void                Dispose() {}
        public  void                ClearPool() {}
    }
    
    public sealed class SyncConnectionError : ISyncConnection {
        public  TaskExecuteError    Error   { get; }
        public  bool                IsOpen  => false;
        public  void                Dispose() { }
        public  void                ClearPool() {}
        
        public SyncConnectionError(TaskExecuteError error) {
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }
        
        public SyncConnectionError(string error) {
            Error = new TaskExecuteError(TaskErrorType.DatabaseError, error);
        }
        
        public SyncConnectionError(Exception exception) {
            Error = new TaskExecuteError(exception.Message);
        }
        
        public static SyncConnectionError DatabaseDoesNotExist(string dbName) {
            var msg = $"database does not exist: '{dbName}'\nTo create one call: database.{nameof(EntityDatabase.SetupDatabaseAsync)}()";
            return new SyncConnectionError(msg);
        }
    }
    
    internal readonly struct ConnectionScope : IDisposable
    {
        internal readonly   ISyncConnection   instance;
        private  readonly   EntityDatabase    database;
        
        internal ConnectionScope(ISyncConnection instance, EntityDatabase database) {
            this.instance   = instance ?? throw new ArgumentNullException(nameof(instance));
            if (!instance.IsOpen) throw new ArgumentException("Expect open connection", nameof(instance));
            this.database   = database;
        }
        public void Dispose() {
            if (instance == null) {
                return;
            }
            database.ReturnConnection(instance);
        }
    }
}