using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host.Event.Collector
{
    /// <summary>
    /// Combine container changes - create, upsert, merge and delete - stored in <see cref="EventCollector"/>
    /// </summary>
    /// <remarks>
    /// A <see cref="ChangeCombiner"/> is mainly used for optimization.<br/>
    /// It combines multiple change tasks - eg. upsert - send from various clients into a single task. <br/>
    /// This enables sending only a single tasks to subscribed clients instead of sending each change individually. <br/>
    /// The effects are: <br/>
    /// - size of serialized events sent to clients is smaller. <br/>
    /// - serialized events are easier to read by humans. <br/>
    /// - the CPU / memory cost to serialize events can reduce to O(1) instead O(N) for all clients having
    ///   the same <see cref="DatabaseSubs"/> - See <see cref="rawSyncEvents"/><br/>
    /// <see cref="ChangeCombiner"/> is not thread safe.
    /// </remarks>
    internal class ChangeCombiner
    {
        private  readonly   EventCollector                      collector;
        private  readonly   List<DatabaseChanges>               databaseChangesList;
        private  readonly   HashSet<ContainerChanges>           containerChangesSet;
        internal readonly   MemoryBuffer                        rawTaskBuffer;
        internal readonly   WriteTaskModel                      writeTaskModel;
        internal readonly   DeleteTaskModel                     deleteTaskModel;
        private             SyncEvent                           syncEvent;
        private  readonly   Dictionary<DatabaseSubs, JsonValue> rawSyncEvents;
        
        internal ChangeCombiner(EventCollector collector) {
            this.collector      = collector;
            databaseChangesList = new List<DatabaseChanges>();
            containerChangesSet = new HashSet<ContainerChanges>();
            rawTaskBuffer       = new MemoryBuffer(1024);
            writeTaskModel      = new WriteTaskModel();
            deleteTaskModel     = new DeleteTaskModel();
            syncEvent           = new SyncEvent { tasksJson = new List<JsonValue>() };
            rawSyncEvents       = new Dictionary<DatabaseSubs, JsonValue>();
        }
        
        /// <summary>
        /// Accumulate all container changes - create, upsert, merge and delete - for the <see cref="ClientDbSubs"/>
        /// of each database passed in <paramref name="databaseSubsMap"/>
        /// </summary>
        /// <remarks>
        /// <b>Note</b> Method is not thread safe
        /// </remarks>
        internal void AccumulateChanges(DatabaseSubsMap databaseSubsMap, ObjectWriter writer)
        {
            collector.GetDatabaseChanges(databaseChangesList);
            
            var context = new CombinerContext(this, writer);
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
                    container.currentType = default;
                }
                var clientDbSubs = databaseSubsMap.map[databaseChanges.dbName];
                EnqueueSyncEvents(clientDbSubs, writer);
                foreach (var pair in databaseChanges.containers) {
                    pair.Value.Reset();
                }
            }
        }
        
        /// <summary>
        /// Create a serialized <see cref="SyncEvent"/>'s for the <see cref="containerChangesSet"/>
        /// and queue them to the passed <paramref name="clientDbSubs"/>
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
                    // todo consider / benchmark creating & queuing a serialized EventMessage for clients have the same set of DatabaseSubs
                    clientDbSub.client.EnqueueSyncEvent(rawSyncEvent);
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