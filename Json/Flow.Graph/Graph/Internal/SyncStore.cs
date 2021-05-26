// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

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
    }
}