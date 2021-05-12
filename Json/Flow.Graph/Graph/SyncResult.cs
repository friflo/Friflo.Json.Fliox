// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Flow.Graph
{
    public class SyncResult
    {
        public readonly List<SyncTask>  tasks;
        public readonly List<SyncTask>  failed;

        public          bool            Success => failed.Count == 0;
        public          string          Message => GetMessage(failed);

        public override string          ToString() => $"tasks: {tasks.Count}, failed: {failed.Count}";

        internal SyncResult(List<SyncTask> tasks, List<SyncTask> failed) {
            this.tasks  = tasks;
            this.failed = failed;
        }
        
        internal static string GetMessage(List<SyncTask> failed) {
            var sb = new StringBuilder();
            sb.Append("Sync() failed with task errors. Count: ");
            sb.Append(failed.Count);
            foreach (var task in failed) {
                sb.Append("\n| ");
                sb.Append(task.Label); // todo should use appender instead of Label
                sb.Append(" - ");
                var error = task.GetTaskError();
                error.AppendAsText("| ", sb, 3);
            }
            return sb.ToString();
        }
    }
}