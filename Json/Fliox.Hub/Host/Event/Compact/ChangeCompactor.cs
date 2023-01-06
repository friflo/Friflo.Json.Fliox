// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Friflo.Json.Fliox.Hub.Host.Event.Compact
{
    /// <summary>
    /// Compact (accumulate) entity change tasks - create, upsert, merge and delete - for registered
    /// <see cref="EntityDatabase"/>'s <br/>
    /// </summary>
    /// <remarks>
    /// A <see cref="ChangeCompactor"/> is mainly used for optimization.<br/>
    /// It combines multiple change tasks - eg. upsert - send from various clients into a single task. <br/>
    /// This enables sending only a single tasks to subscribed clients instead of sending each change individually. <br/>
    /// The effects are: <br/>
    /// - size of serialized events sent to clients is smaller. <br/>
    /// - serialized events are easier to read by humans. <br/>
    /// - the CPU / memory cost to serialize events can reduce to O(1) instead O(N) for all clients having
    ///   the same <see cref="DatabaseSubs"/> - See <see cref="rawSyncEvents"/><br/>
    /// </remarks>
    internal sealed partial class ChangeCompactor
    {
        /// <summary>Thread safe map used to collect the <see cref="DatabaseChanges"/> for each database</summary>
        private  readonly   Dictionary<EntityDatabase, DatabaseChanges> databaseChangesMap;
        internal            int                                         DatabaseCount { get; private set; }
        /// <summary>
        /// Fields below are used as buffers in <see cref="AccumulateTasks"/> which is not thread safe.
        /// </summary>
        private  readonly   List<DatabaseChanges>               databaseChangesList;
        private  readonly   HashSet<ContainerChanges>           containerChangesSet;
        internal readonly   MemoryBuffer                        rawTaskBuffer;
        internal readonly   WriteTaskModel                      writeTaskModel;
        internal readonly   DeleteTaskModel                     deleteTaskModel;
        private             SyncEvent                           syncEvent;
        private  readonly   Dictionary<DatabaseSubs, JsonValue> rawSyncEvents;
        
        public ChangeCompactor() {
            databaseChangesMap  = new Dictionary<EntityDatabase, DatabaseChanges>();
            databaseChangesList = new List<DatabaseChanges>();
            containerChangesSet = new HashSet<ContainerChanges>();
            rawTaskBuffer       = new MemoryBuffer(1024);
            writeTaskModel      = new WriteTaskModel();
            deleteTaskModel     = new DeleteTaskModel();
            syncEvent           = new SyncEvent { tasksJson = new List<JsonValue>() };
            rawSyncEvents       = new Dictionary<DatabaseSubs, JsonValue>();
        }
        
        internal void AddDatabase(EntityDatabase database) {
            var databaseChanges = new DatabaseChanges(database.name);
            lock (databaseChangesMap) {
                databaseChangesMap.Add(database, databaseChanges);
                DatabaseCount = databaseChangesMap.Count;
            }
        }
        
        internal void RemoveDatabase(EntityDatabase database) {
            lock (databaseChangesMap) {
                databaseChangesMap.Remove(database);
                DatabaseCount = databaseChangesMap.Count;
            }
        } 

        /// <summary>
        /// Accumulate all container changes - create, upsert, merge and delete - for the <see cref="ClientDbSubs"/>
        /// of each database passed in <paramref name="databaseSubsMap"/>
        /// </summary>
        /// <remarks>
        /// <b>Note</b> Method is not thread safe
        /// </remarks>
        internal void AccumulateTasks(DatabaseSubsMap databaseSubsMap, ObjectWriter writer)
        {
            databaseChangesList.Clear();
            lock (databaseChangesMap) {
                foreach (var pair in databaseChangesMap) {
                    var databaseChanges = pair.Value;
                    databaseChanges.SwapBuffers();
                    databaseChangesList.Add(databaseChanges);
                }
            }
            var context = new CompactorContext(this, writer);
            foreach (var databaseChanges in databaseChangesList)
            {
                syncEvent.db = databaseChanges.dbName.value;
                containerChangesSet.Clear();
                rawTaskBuffer.Reset();
                var readBuffer  = databaseChanges.readBuffer;
                foreach (var changeTask in readBuffer.changeTasks) {
                    changeTask.containerChanges.AddChangeTask(changeTask, readBuffer, context);
                    containerChangesSet.Add(changeTask.containerChanges);
                }
                if (containerChangesSet.Count == 0) {
                    continue;
                }
                foreach (var container in containerChangesSet) {
                    container.AddAccumulatedRawTask(context);
                    container.currentType = TaskType.error;
                }
                var clientDbSubs = databaseSubsMap.map[databaseChanges.dbName];
                EnqueueSyncEvents(clientDbSubs, writer);
                foreach (var pair in databaseChanges.containers) {
                    pair.Value.Reset();
                }
            }
        }
        
        /// <summary>
        /// Create a serialized <see cref="SyncEvent"/>'s and queue them to the passed <paramref name="clientDbSubs"/>
        /// </summary>
        private void EnqueueSyncEvents(ClientDbSubs[] clientDbSubs, ObjectWriter writer) {
            foreach (var containerChanges in containerChangesSet) {
                rawSyncEvents.Clear();
                foreach (var clientDbSub in clientDbSubs) {
                    var databaseSubs = clientDbSub.subs;
                    if (!rawSyncEvents.TryGetValue(databaseSubs, out var rawSyncEvent)) {
                        rawSyncEvent = CreateSyncEvent(containerChanges, databaseSubs.changeSubs, writer);
                        rawSyncEvents.Add(databaseSubs, rawSyncEvent);
                    }
                    if (rawSyncEvent.IsNull()) {
                        continue;
                    }
                    clientDbSub.client.EnqueueEvent(rawSyncEvent);
                }
            }
        }
        
        /// <summary>
        /// Create a serialized <see cref="SyncEvent"/> for the passed <paramref name="container"/>
        /// and <paramref name="changeSubs"/> <br/>
        /// Return default if no <see cref="SyncEvent"/> was created 
        /// </summary>
        private JsonValue CreateSyncEvent(ContainerChanges container, ChangeSub[] changeSubs, ObjectWriter writer)
        {
            syncEvent.tasksJson.Clear();
            foreach (var changeSub in changeSubs) {
                // todo could check matching container by using EntityContainer reference
                if (!changeSub.container.IsEqual(container.name)) {
                    continue;
                }
                // found matching container subscription => add matching raw tasks
                foreach (var rawTask in container.rawTasks) {
                    if ((changeSub.changes & rawTask.change) == 0) {
                        continue;
                    }
                    syncEvent.tasksJson.Add(rawTask.value);
                }
                break;
            }
            if (syncEvent.tasksJson.Count == 0) {
                return default;
            }
            return RemoteUtils.SerializeSyncEvent(syncEvent, writer);
        }
    }
}