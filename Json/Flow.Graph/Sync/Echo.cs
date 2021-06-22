// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class Echo : DatabaseTask
    {
        public              string          message;
            
        internal override   TaskType        TaskType    => TaskType.echo;
        public   override   string          ToString()  => message;

        internal override Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            TaskResult result = new EchoResult{message = message};
            return Task.FromResult(result); 
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class EchoResult : TaskResult, ICommandResult
    {
        public              string          message;
        
        public CommandError                 Error { get; set; }

        internal override   TaskType        TaskType => TaskType.echo;
    }
}