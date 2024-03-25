// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    /// <summary>
    /// WIP
    /// </summary>
    public sealed class ReserveKeys  : SyncRequestTask {
        [Serialize                        ("cont")]
        [Required]  public  ShortString     container;
        [Required]  public  int             count;
        
        public   override   TaskType        TaskType => TaskType.reserveKeys;
        public   override   string          TaskName => $"container: '{container}'";

        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var hub = syncContext.Hub;
            // var store           = new SequenceStore(database, SyncTypeStore.Get(), null);
            var pool = syncContext.pool;
            using (var pooledStore = pool.Type(() => new SequenceStore(hub)).Get()) {
                var store = pooledStore.instance;
                store.UserId = "ReserveKeys";
                var read            = store.sequence.Read();
                var containerKey    = new JsonKey(container);
                var sequenceTask    = read.Find(containerKey);
                var sync            = await store.TrySyncTasks().ConfigureAwait(false);
                if (!sync.Success) {
                    return  new ReserveKeysResult { Error = new TaskExecuteError(sync.Message) };
                }
                var sequence = sequenceTask.Result;
                if (sequence == null) {
                    sequence = new Sequence {
                        container   = new JsonKey(container),
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
                    return  new ReserveKeysResult { Error = new TaskExecuteError(sync.Message) };
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
        
        public override SyncTaskResult Execute (EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// WIP
    /// </summary>
    public sealed class ReserveKeysResult : SyncTaskResult
    {
                    public  ReservedKeys?       keys;
        
        [Ignore]    public  TaskExecuteError    Error       { get; set; }
        internal override   TaskType            TaskType    => TaskType.reserveKeys;
        internal override   bool                Failed      => Error != null;
    }
    
    /// <summary>
    /// WIP
    /// </summary>
    public struct ReservedKeys
    {
        [Required]  public  long    start;
        [Required]  public  int     count;
        [Required]  public  Guid    token;
    }
}