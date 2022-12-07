// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Contains the result of <see cref="FlioxClient.SyncTasks"/> / <see cref="FlioxClient.TrySyncTasks"/>
    /// </summary>
    public sealed class SyncResult
    {
        public              IReadOnlyList<SyncFunction> Functions  => functions;
        public              IReadOnlyList<SyncFunction> Failed     => GetFailed();
        
        private  readonly   FlioxClient                 client;
        private  readonly   List<SyncFunction>          functions;
        internal readonly   List<SyncFunction>          failed;
        private             ErrorResponse               errorResponse;

        public              bool                        Success     => failed == null && errorResponse == null;
        public              string                      Message     => GetMessage(errorResponse, failed);

        public override     string                      ToString()  => $"tasks: {functions.Count}, failed: {failed.Count}";
        
        internal SyncResult(
            FlioxClient         client,
            List<SyncFunction>  tasks,
            List<SyncFunction>  failed,
            ErrorResponse       errorResponse)
        {
            this.client         = client;
            this.errorResponse  = errorResponse;
            this.functions      = tasks;
            this.failed         = failed;
        }
        
        public void ReUse() {
            foreach (var function in functions) {
                function.ReUse();
            }
            functions.Clear();
            failed?.Clear();
            errorResponse = null;
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