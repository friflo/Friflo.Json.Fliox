// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

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
    public sealed partial class ChangeCompactor
    {
        private  readonly   Dictionary<EntityDatabase, DatabaseChanges> databaseChangesMap;
        private  readonly   List<DatabaseChanges>                       databaseChangesList;
        private  readonly   HashSet<ContainerChanges>                   containerChangesSet;
        internal readonly   MemoryBuffer                                rawTaskBuffer;
        internal readonly   WriteTaskModel                              writeTaskModel;
        internal readonly   DeleteTaskModel                             deleteTaskModel;
        private  readonly   SyncEvent                                   syncEvent;
        private  readonly   Dictionary<DatabaseSubs, JsonValue>         rawSyncEvents;
        
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
        
        public void AddDatabase(EntityDatabase database) {
            var databaseChanges = new DatabaseChanges(database.name);
            lock (databaseChangesMap) {
                databaseChangesMap.Add(database, databaseChanges);
            }
        } 

        internal void AccumulateTasks(DatabaseSubsMap databaseSubsMap, ObjectWriter writer)
        {
            databaseChangesList.Clear();
            lock (databaseChangesMap) {
                foreach (var pair in databaseChangesMap) {
                    var databaseChanges = pair.Value;
                    databaseChanges.SwapBuffers();
                    databaseChangesList.Add(pair.Value);
                }
            }
            var context = new CompactorContext(this, writer);
            foreach (var databaseChanges in databaseChangesList)
            {
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
        /// Create a serialized <see cref="SyncEvent"/>'s for the passed <paramref name="clientDbSubs"/> array
        /// </summary>
        private void EnqueueSyncEvents(ClientDbSubs[] clientDbSubs, ObjectWriter writer) {
            if (clientDbSubs.Length == 0) {
                return;
            }
            foreach (var containerChanges in containerChangesSet) {
                rawSyncEvents.Clear();
                foreach (var clientDbSub in clientDbSubs) {
                    var databaseSubs = clientDbSub.subs;
                    if (rawSyncEvents.TryGetValue(databaseSubs, out var rawSyncEvent)) {
                        if (rawSyncEvent.IsNull()) {
                            continue;
                        }
                        clientDbSub.client.EnqueueEvent(rawSyncEvent);
                        continue;
                    }
                    rawSyncEvent = EnqueueSyncEvent(containerChanges, clientDbSub, writer);
                    rawSyncEvents.Add(databaseSubs, rawSyncEvent);
                }
            }
        }
        
        /// <summary>
        /// Create a serialized <see cref="SyncEvent"/> for the passed <paramref name="container"/>
        /// and <paramref name="clientDbSubs"/> <br/>
        /// Return default if no <see cref="SyncEvent"/> was created 
        /// </summary>
        private JsonValue EnqueueSyncEvent(
            ContainerChanges    container,
            in ClientDbSubs     clientDbSubs,
            ObjectWriter        writer)
        {
            syncEvent.tasksJson.Clear();
            foreach (var changeSub in clientDbSubs.subs.changeSubs) {
                if (!changeSub.container.IsEqual(container.name)) {
                    continue;
                }
                foreach (var rawTask in container.rawTasks) {
                    if ((changeSub.changes & rawTask.change) == 0) {
                        continue;
                    }
                    syncEvent.tasksJson.Add(rawTask.value);
                }
            }
            if (syncEvent.tasksJson.Count == 0) {
                return default;
            }
            var rawSyncEvent = RemoteUtils.SerializeSyncEvent(syncEvent, writer);
            clientDbSubs.client.EnqueueEvent(rawSyncEvent);
            return rawSyncEvent;
        }
    }
}