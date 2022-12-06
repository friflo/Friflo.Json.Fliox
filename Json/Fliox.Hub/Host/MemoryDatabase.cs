// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// A <see cref="MemoryDatabase"/> is a non-persistent database used to store records in memory.
    /// </summary>
    /// <remarks>
    /// The intention is having a shared database which can be used in high performance scenarios. <br/>
    /// E.g. on a 4 Core CPU it is able to achieve more than 500.000 request / second. <br/>
    /// Following use-cases are suitable for a <see cref="MemoryDatabase"/>
    /// <list type="bullet">
    ///   <item>Run a big amount of unit tests fast and efficient as instantiation of <see cref="MemoryDatabase"/> take only some micro seconds. </item>
    ///   <item>Use as a Game Session database for online multiplayer games as it provide sub millisecond response latency</item>
    ///   <item>Use as test database for <b>TDD</b> without any configuration </item>
    ///   <item>Is the benchmark reference for all other database implementations regarding throughput and latency</item>
    /// </list>
    /// <see cref="MemoryDatabase"/> has no third party dependencies.
    /// <i>Storage characteristics</i> <br/>
    /// <b>Keys</b> are stored as <see cref="JsonKey"/> - keys that can be converted to <see cref="long"/> or <see cref="Guid"/>
    /// are stored without heap allocation. Otherwise a <see cref="string"/> is allocated <br/>
    /// <b>Values</b> are stored as <see cref="JsonValue"/> - essentially a <see cref="byte"/>[]
    /// </remarks>
    public sealed class MemoryDatabase : EntityDatabase
    {
        private  readonly   bool        pretty;
        private  readonly   MemoryType  containerType;
        private  readonly   int         smallValueSize;
        
        public   override   string      StorageType             => "in-memory";

        /// <param name="dbName"></param>
        /// <param name="service"></param>
        /// <param name="type"></param>
        /// <param name="opt"></param>
        /// <param name="pretty"></param>
        /// <param name="smallValueSize"> Intended for write heavy containers. <br/>
        /// Byte arrays used to store container values are reused in case their length is less or equal this size. 
        /// </param>
        public MemoryDatabase(
            string          dbName,
            DatabaseService service         = null,
            MemoryType?     type            = null,
            DbOpt           opt             = null,
            bool            pretty          = false,
            int             smallValueSize  = -1)
            : base(dbName, service, opt)
        {
            this.pretty         = pretty;
            this.smallValueSize = smallValueSize;
            containerType       = type ?? MemoryType.Concurrent;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new MemoryContainer(name, database, containerType, pretty, smallValueSize);
        }
        
        public override bool PreExecute(SyncRequestTask task) {
            switch (task.TaskType) {
                case TaskType.create:
                case TaskType.upsert:
                case TaskType.merge:
                case TaskType.delete:
                    return true;
                case  TaskType.read:
                    var read = (ReadEntities)task;
                    return read.references == null;
                case TaskType.message:
                case TaskType.command:
                    return ((SyncMessageTask)task).PreExecute(service);
            }
            return false;
        }
    }
    
    public enum MemoryType {
        Concurrent,
        /// used to preserve insertion order of entities in ClusterDB and MonitorDB
        NonConcurrent
    }
    
    internal sealed class MemoryQueryEnumerator : QueryEnumerator
    {
        private readonly IEnumerator<KeyValuePair<JsonKey, JsonValue>>  enumerator;
        
        internal MemoryQueryEnumerator(IDictionary<JsonKey, JsonValue> map) {
            enumerator = map.GetEnumerator();
        }

        public override bool MoveNext() {
            return enumerator.MoveNext();
        }

        public override JsonKey Current => enumerator.Current.Key;

        protected override void DisposeEnumerator() {
            enumerator.Dispose();
        }
    }
}