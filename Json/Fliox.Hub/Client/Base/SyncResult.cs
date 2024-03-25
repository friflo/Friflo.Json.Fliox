// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Utils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Contains the result of <see cref="FlioxClient.SyncTasks"/> / <see cref="FlioxClient.TrySyncTasks"/>
    /// </summary>
    public sealed class SyncResult
    {
        public              IReadOnlyList<SyncTask>     Tasks       => tasks;
        public              IReadOnlyList<SyncTask>     Failed      => GetFailed();
        private             SyncStore                   syncStore;
        private             MemoryBuffer                memoryBuffer;
        private             ListOne<SyncTask>           tasks;
        internal            ListOne<SyncTask>           failed;
        private             ErrorResponse               errorResponse;

        public              bool                        Success     => failed == null && errorResponse == null;
        public              string                      Message     => GetMessage(errorResponse, failed);

        public override     string                      ToString()  => $"tasks: {tasks.Count}, failed: {failed.Count}";

#pragma warning disable CS0169
        private  readonly   FlioxClient                 client; // only set in DEBUG to avoid client not being collected by GC
#pragma warning restore CS0169
        
        internal SyncResult(FlioxClient client) {
#if DEBUG
            this.client = client;
#endif
        }
        
        internal void Init (
            SyncStore               syncStore,
            MemoryBuffer            memoryBuffer,
            ListOne<SyncTask>       tasks,
            ListOne<SyncTask>       failed,
            ErrorResponse           errorResponse)
        {
            this.syncStore      = syncStore;
            this.memoryBuffer   = memoryBuffer;
            this.tasks          = tasks;
            this.failed         = failed;
            this.errorResponse  = errorResponse;
        }
        
        public void Reuse(FlioxClient client) {
#if DEBUG
            if (client != this.client) throw new InvalidOperationException("SyncResult was created by different client");
#endif
            syncStore.Reuse();
            client._intern.syncStoreBuffer.Add(syncStore);
            syncStore       = null;

            client._intern.memoryBufferPool.Add(memoryBuffer);
            memoryBuffer    = null;

            errorResponse   = null;
            tasks           = null;
            failed          = null;
            client._intern.syncResultBuffer.Add(this);
        }
        
        private IReadOnlyList<SyncTask> GetFailed() {
            if (failed != null)
                return failed;
            return Array.Empty<SyncTask>();
        }
        
        internal static string GetMessage(ErrorResponse errorResponse, ListOne<SyncTask> failed) {
            if (errorResponse != null) {
                return errorResponse.message;
            }
            var sb = new StringBuilder();
            sb.Append("SyncTasks() failed with task errors. Count: ");
            if (failed == null) {
                sb.Append('0');
            } else {
                sb.Append(failed.Count);
                foreach (var task in failed.GetReadOnlySpan()) {
                    sb.Append("\n|- ");
                    sb.Append(task.GetLabel()); // todo should use appender instead of Label
                    sb.Append(" # ");
                    var taskError = task.Error;
                    taskError.AppendAsText("|   ", sb, 3, true);
                }
            }
            return sb.ToString();
        }
    }
}