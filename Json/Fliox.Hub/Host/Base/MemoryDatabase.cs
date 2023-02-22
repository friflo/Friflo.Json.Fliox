// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static Friflo.Json.Fliox.Hub.Protocol.Tasks.TaskType;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// A <see cref="MemoryDatabase"/> is a non-persistent database used to store records in memory.
    /// </summary>
    /// <remarks>
    /// The intention is having a shared database which can be used in high performance scenarios. E.g:<br/>
    /// 4 Core i7-4790K CPU 4.00GHz: 1.000.000 requests / second. <br/>
    /// Mac Mini M2:                 1.800.000 requests / second. <br/>
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
        public              bool        Pretty          { get; init; } = false;
        public              MemoryType  ContainerType   { get; init; } = MemoryType.Concurrent;
        /// <summary>Intended for write heavy containers.</summary>
        public              int         SmallValueSize  { get; init; } = -1;
        
        public   override   string      StorageType             => "in-memory";

        /// <param name="dbName"></param>
        /// <param name="service"></param>
        /// Byte arrays used to store container values are reused in case their length is less or equal this size. 
        public MemoryDatabase(string dbName, DatabaseService service = null)
            : base(dbName, service)
        { }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new MemoryContainer(name.AsString(), database, ContainerType, Pretty, SmallValueSize);
        }
        
        public override bool IsSyncTask(SyncRequestTask task) {
            switch (task.TaskType) {
                case create:
                case upsert:
                case merge:
                case delete:
                case read:
                case subscribeChanges:
                case subscribeMessage:
                    return true;
            }
            return false;
        }
    }
    
    public enum MemoryType {
        Concurrent      = 1,
        /// used to preserve insertion order of entities in ClusterDB and MonitorDB
        NonConcurrent   = 2
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