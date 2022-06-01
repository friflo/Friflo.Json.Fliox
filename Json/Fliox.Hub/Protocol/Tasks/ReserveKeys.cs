// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Req = Friflo.Json.Fliox.RequiredMemberAttribute;
using Ignore = Friflo.Json.Fliox.IgnoreMemberAttribute;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    /// <summary>
    /// WIP
    /// </summary>
    public sealed class ReserveKeys  : SyncRequestTask {
        [Req]  public  string          container;
        [Req]  public  int             count;
        
        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var hub = syncContext.Hub;
            // var store           = new SequenceStore(database, SyncTypeStore.Get(), null);
            var pool = syncContext.pool;
            using (var pooledStore = pool.Type(() => new SequenceStore(hub)).Get()) {
                var store = pooledStore.instance;
                store.UserId = "ReserveKeys";
                var read            = store.sequence.Read();
                var sequenceTask    = read.Find(container);
                var sync            = await store.TrySyncTasks().ConfigureAwait(false);
                if (!sync.Success) {
                    return  new ReserveKeysResult { Error = new CommandError(sync.Message) };
                }
                var sequence = sequenceTask.Result;
                if (sequence == null) {
                    sequence = new Sequence {
                        container   = container,
                        autoId      = count 
                    };
                } else {
                    sequence.autoId += count;
                }
                var sequenceKeys = new SequenceKeys {
                    token       = Guid.NewGuid(),
                    container   = container,
                    start       = sequence.autoId,
                    count       = count,
                    user        = syncContext.clientId
                };
                store.sequenceKeys.Upsert(sequenceKeys);
                store.sequence.Upsert(sequence);
                sync = await store.TrySyncTasks().ConfigureAwait(false);
                if (!sync.Success) {
                    return  new ReserveKeysResult { Error = new CommandError(sync.Message) };
                }
                var keys = new ReservedKeys {
                    start = sequence.autoId,
                    count = count,
                    token = sequenceKeys.token
                };
                var result = new ReserveKeysResult { keys = keys };
                return result;
            }
        }

        internal override       TaskType        TaskType => TaskType.reserveKeys;
        public   override       string          TaskName => $"container: '{container}'";
    }
    
    /// <summary>
    /// WIP
    /// </summary>
    public sealed class ReserveKeysResult : SyncTaskResult {
                    public  ReservedKeys?   keys;
        
        [Ignore]    public  CommandError    Error { get; set; }
        internal override   TaskType        TaskType => TaskType.reserveKeys;
    }
    
    /// <summary>
    /// WIP
    /// </summary>
    public struct ReservedKeys
    {
        [Req]  public  long    start;
        [Req]  public  int     count;
        [Req]  public  Guid    token;
    }
}