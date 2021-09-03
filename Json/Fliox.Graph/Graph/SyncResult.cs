// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Db.Sync;

namespace Friflo.Json.Fliox.Db.Graph
{
    public class SyncResult
    {
        public  readonly    List<SyncTask>  tasks;
        public  readonly    List<SyncTask>  failed;
        
        private readonly    ErrorResponse   errorResponse;


        public              bool            Success => failed.Count == 0 && errorResponse == null;
        public              string          Message => GetMessage(errorResponse, failed);

        public override string          ToString() => $"tasks: {tasks.Count}, failed: {failed.Count}";
        
        internal SyncResult(List<SyncTask> tasks, List<SyncTask> failed, ErrorResponse errorResponse) {
            this.errorResponse  = errorResponse;
            this.tasks      = tasks;
            this.failed     = failed;
        }
        
        internal static string GetMessage(ErrorResponse errorResponse, List<SyncTask> failed) {
            if (errorResponse != null) {
                return errorResponse.message;
            }
            var sb = new StringBuilder();
            sb.Append("Sync() failed with task errors. Count: ");
            sb.Append(failed.Count);
            foreach (var task in failed) {
                sb.Append("\n|- ");
                sb.Append(task.GetLabel()); // todo should use appender instead of Label
                sb.Append(" # ");
                var taskError = task.Error;
                taskError.AppendAsText("|   ", sb, 3, false);
            }
            return sb.ToString();
        }
    }
}