// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal struct SetIntern<TKey, T> where T : class
    {
        internal readonly   FlioxClient         store;
        private  readonly   EntitySet<TKey,T>   entitySet;
        internal readonly   bool                autoIncrement;
        private             TypeMapper<T>       typeMapper;     // set/create on demand
        private             List<TKey>          keysBuf;        // create on demand

        
        // --- non readonly
        internal            SubscribeChanges    subscription;
        internal            bool                writePretty;
        internal            bool                writeNull;
        
        public    override  string              ToString()  => "";
        
        internal            SyncSet             SyncSet => entitySet.syncSet;
        
        internal List<TKey>     GetKeysBuf()    => keysBuf      ?? (keysBuf = new List<TKey>());
        internal TypeMapper<T>  GetMapper()     => typeMapper   ?? (typeMapper = (TypeMapper<T>)store._intern.typeStore.GetTypeMapper(typeof(T)));

        internal SetIntern(FlioxClient store, EntitySet<TKey,T> entitySet) {
            typeMapper      = null;
            this.store      = store;
            this.entitySet  = entitySet;
            subscription    = null;
            keysBuf         = null;
            autoIncrement   = EntitySet<TKey,T>.EntityKeyTMap.autoIncrement;
            writePretty     = EntitySet.DefaultWritePretty;
            writeNull       = EntitySet.DefaultWriteNull;
        }
    }
}
