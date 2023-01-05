// Copyright (c) Ullrich Praetz. All rights reserved.
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
        public              IReadOnlyList<SyncFunction> Functions  => functions;
        public              IReadOnlyList<SyncFunction> Failed     => GetFailed();
        
        private             SyncRequest                 syncRequest;
        private             SyncStore                   syncStore;
        private             MemoryBuffer                memoryBuffer;
        private             List<SyncFunction>          functions;
        internal            List<SyncFunction>          failed;
        private             ErrorResponse               errorResponse;

        public              bool                        Success     => failed == null && errorResponse == null;
        public              string                      Message     => GetMessage(errorResponse, failed);

        public override     string                      ToString()  => $"tasks: {functions.Count}, failed: {failed.Count}";

#pragma warning disable CS0169
        private  readonly   FlioxClient                 client; // only set in DEBUG to avoid client not being collected by GC
#pragma warning restore CS0169
        
        internal SyncResult(FlioxClient client) {
#if DEBUG
            this.client = client;
#endif
        }
        
        internal void Init (
            SyncRequest         syncRequest,
            SyncStore           syncStore,
            MemoryBuffer        memoryBuffer,
            List<SyncFunction>  tasks,
            List<SyncFunction>  failed,
            ErrorResponse       errorResponse)
        {
            this.syncRequest    = syncRequest;
            this.syncStore      = syncStore;
            this.memoryBuffer   = memoryBuffer;
            this.functions      = tasks;
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
            
            syncRequest.tasks.Clear();
            client._intern.syncRequestBuffer.Add(syncRequest);
            syncRequest     = null;

            client._intern.memoryBufferPool.Add(memoryBuffer);
            memoryBuffer    = null;

            errorResponse   = null;
            functions       = null;
            failed          = null;
            client._intern.syncResultBuffer.Add(this);
        }
        
        private IReadOnlyList<SyncFunction> GetFailed() {
            if (failed != null)
                return failed;
            return Array.Empty<SyncFunction>();
        }
        
        internal static string GetMessage(ErrorResponse errorResponse, List<SyncFunction> failed) {
            if (errorResponse != null) {
                return errorResponse.message;
            }
            var sb = new StringBuilder();
            sb.Append("SyncTasks() failed with task errors. Count: ");
            if (failed == null) {
                sb.Append('0');
            } else {
                sb.Append(failed.Count);
                foreach (var task in failed) {
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