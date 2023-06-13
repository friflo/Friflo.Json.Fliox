// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    /// <summary>
    /// <see cref="SyncConnection"/> instances are created on demand per <see cref="SyncContext"/>.<br/>
    /// This enables:<br/>
    /// <list type="bullet">
    ///   <item>Execution of multiple databases commands simultaneously</item>
    ///   <item>Execute a set of database commands (tasks) inside a transaction</item>
    /// </list> 
    /// </summary>
    public sealed class SyncConnection
    {
        public  readonly    IDisposable         instance;
        public  readonly    TaskExecuteError    error;
        
        public              bool                Failed      => error != null;
        public  override    string              ToString()  => GetString();

        public SyncConnection(IDisposable instance) {
            this.instance   = instance ?? throw new ArgumentNullException(nameof(instance));
            error           = null;
        }
        
        public SyncConnection(TaskExecuteError error) {
            instance        = default;
            this.error      = error ?? throw new ArgumentNullException(nameof(error));
        }
        
        public SyncConnection(Exception exception) {
            instance        = default;
            error           = new TaskExecuteError(exception.Message);
        }

        public void Dispose() {
            instance?.Dispose();
        }
        
        private string GetString() {
            if (instance != null) {
                return "Connected";
            }
            return $"Error: {error.message}";
        }
    }
}