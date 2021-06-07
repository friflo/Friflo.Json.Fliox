// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    public class SyncResult
    {
        public  readonly    List<SyncTask>  tasks;
        public  readonly    List<SyncTask>  failed;
        
        private readonly    ResponseError   responseError;


        public              bool            Success => failed.Count == 0 && responseError == null;
        public              string          Message => GetMessage(responseError, failed);

        public override string          ToString() => $"tasks: {tasks.Count}, failed: {failed.Count}";
        
        internal SyncResult(List<SyncTask> tasks, List<SyncTask> failed, ResponseError responseError) {
            this.responseError  = responseError;
            this.tasks      = tasks;
            this.failed     = failed;
        }
        
        internal static string GetMessage(ResponseError responseError, List<SyncTask> failed) {
            if (responseError != null) {
                return responseError.message;
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