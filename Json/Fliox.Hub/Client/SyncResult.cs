// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Client
{
    public sealed class SyncResult
    {
        public  readonly    List<SyncFunction>  functions;
        public  readonly    List<SyncFunction>  failed;
        
        private readonly    ErrorResponse       errorResponse;


        public              bool                Success => failed.Count == 0 && errorResponse == null;
        public              string              Message => GetMessage(errorResponse, failed);

        public override string          ToString() => $"tasks: {functions.Count}, failed: {failed.Count}";
        
        internal SyncResult(List<SyncFunction> tasks, List<SyncFunction> failed, ErrorResponse errorResponse) {
            this.errorResponse  = errorResponse;
            this.functions          = tasks;
            this.failed         = failed;
        }
        
        internal static string GetMessage(ErrorResponse errorResponse, List<SyncFunction> failed) {
            if (errorResponse != null) {
                return errorResponse.message;
            }
            var sb = new StringBuilder();
            sb.Append("SyncTasks() failed with task errors. Count: ");
            sb.Append(failed.Count);
            foreach (var task in failed) {
                sb.Append("\n|- ");
                sb.Append(task.GetLabel()); // todo should use appender instead of Label
                sb.Append(" # ");
                var taskError = task.Error;
                taskError.AppendAsText("|   ", sb, 3, true);
            }
            return sb.ToString();
        }
    }
}