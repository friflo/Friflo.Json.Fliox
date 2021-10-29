// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal struct SetIntern<TKey, T> where T : class
    {
        internal readonly   TypeMapper<T>       typeMapper;
        internal readonly   ObjectMapper        jsonMapper;
        private             Tracer              tracer;         // create on demand
        internal readonly   FlioxClient         store;
        private             List<TKey>          keysBuf;        // create on demand
        internal readonly   bool                autoIncrement;

        
        // --- non readonly
        internal            SubscribeChanges    subscription;
        internal            bool                writePretty;
        internal            bool                writeNull;
        
        internal List<TKey> GetKeysBuf()    => keysBuf  ?? (keysBuf = new List<TKey>());
        internal Tracer     GetTracer()     => tracer   ?? (tracer = new Tracer(store._intern.typeCache, store));

        internal SetIntern(FlioxClient store) {
            jsonMapper      = store._intern.jsonMapper;
            typeMapper      = (TypeMapper<T>)store._intern.typeStore.GetTypeMapper(typeof(T));
            tracer          = null;
            this.store      = store;
            subscription    = null;
            keysBuf         = null;
            autoIncrement   = EntitySet<TKey,T>.EntityKeyTMap.autoIncrement;
            writePretty     = false;
            writeNull       = false;
        }
    }
}
