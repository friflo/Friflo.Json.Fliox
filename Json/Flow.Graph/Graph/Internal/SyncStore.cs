// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class SyncStore
    {
        private readonly    List<LogTask>  logTasks  = new List<LogTask>();

        internal LogTask CreateLog() {
            var logTask = new LogTask();
            logTasks.Add(logTask);
            return logTask;
        }

        internal void LogResults() {
            foreach (var logTask in logTasks) {
                logTask.state.Synced = true;
                foreach (var patch in logTask.patches) {

                    
                }
            }
        }
    }
}