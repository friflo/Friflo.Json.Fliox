// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper.Map;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal struct SetIntern<TKey, T> where T : class
    {
        internal readonly   FlioxClient         store;
        [DebuggerBrowsable(Never)]
        private  readonly   EntitySet<TKey,T>   entitySet;
        internal readonly   bool                autoIncrement;
        private             TypeMapper<T>       typeMapper;     // set/create on demand
        private             List<TKey>          keysBuf;        // create on demand

        
        // --- non readonly
        internal            SubscribeChanges    subscription;
        internal            bool                writePretty;
        internal            bool                writeNull;
        
        private static readonly EntityKeyT<TKey, T> EntityKeyTMap       = EntityKey.GetEntityKeyT<TKey, T>();
        
        public    override  string              ToString()  => "";
        
        internal            SyncSet             SyncSet => entitySet.syncSet;
        
        internal List<TKey>     GetKeysBuf()    => keysBuf      ??= new List<TKey>();
        internal TypeMapper<T>  GetTypeMapper() => typeMapper   ??= (TypeMapper<T>)store._intern.typeStore.GetTypeMapper(typeof(T));

        internal SetIntern(FlioxClient store, EntitySet<TKey,T> entitySet) {
            typeMapper      = null;
            this.store      = store;
            this.entitySet  = entitySet;
            subscription    = null;
            keysBuf         = null;
            autoIncrement   = EntityKeyTMap.autoIncrement;
            writePretty     = ClientStatic.DefaultWritePretty;
            writeNull       = ClientStatic.DefaultWriteNull;
        }
    }
}
