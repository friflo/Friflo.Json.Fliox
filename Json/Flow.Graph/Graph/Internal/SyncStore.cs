// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class SyncStore
    {
        internal readonly   List<SyncTask>                  appTasks    = new List<SyncTask>();
        private  readonly   List<LogTask>                   logTasks    = new List<LogTask>();
        internal readonly   Dictionary<string, EchoTask>    echoTasks   = new Dictionary<string, EchoTask>();

        internal LogTask CreateLog() {
            var logTask = new LogTask();
            logTasks.Add(logTask);
            return logTask;
        }

        internal void LogResults() {
            foreach (var logTask in logTasks) {
                logTask.state.Synced = true;
                logTask.SetResult();
            }
        }
        
        // ----------------------------------- add tasks methods -----------------------------------
        internal void AddTasks(List<DatabaseTask> tasks) {
            Echo(tasks);
        }
                
        private void Echo(List<DatabaseTask> tasks) {
            foreach (var entry in echoTasks) {
                EchoTask echoTask = entry.Value;
                var req = new Echo {
                    message   = echoTask.message,
                };
                tasks.Add(req);
            }
        }
        
        internal void EchoResult (Echo task, TaskResult result) {
            EchoTask echoTask = echoTasks[task.message];
            if (result is TaskErrorResult taskError) {
                echoTask.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            var echoResult = (EchoResult)result;
            echoTask.result = echoResult.message;
            echoTask.state.Synced = true;
        }
    }
}