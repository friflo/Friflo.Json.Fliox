// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Sync
{
    public class ReserveKeys  : DatabaseTask {
        [Fri.Required]  public  int             count;
        [Fri.Required]  public  string          container;

        internal override Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            throw new System.NotImplementedException();
        }

        internal override       TaskType        TaskType => TaskType.reserveKeys;
        public   override       string          TaskName => $"container: '{container}'";
    }
    
    public class ReserveKeysResult : TaskResult {
        [Fri.Required]  public  int             start;
        [Fri.Required]  public  int             count;
        [Fri.Required]  public  string          token;
        
                        public  CommandError    Error { get; set; }
        internal override       TaskType        TaskType => TaskType.reserveKeys;
    }
}