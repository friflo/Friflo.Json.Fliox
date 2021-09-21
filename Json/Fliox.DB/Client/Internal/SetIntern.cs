// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.DB.Client.Internal
{
    internal struct SetIntern<TKey, T> where T : class
    {
        internal readonly   TypeMapper<T>       typeMapper;
        internal readonly   ObjectMapper        jsonMapper;
        internal readonly   ObjectPatcher       objectPatcher;
        internal readonly   Tracer              tracer;
        internal readonly   EntityStore         store;
        internal readonly   List<TKey>          keysBuf;
        internal readonly   bool                autoIncrement;

        
        // --- non readonly
        internal            SubscribeChanges    subscription;

        internal SetIntern(EntityStore store) {
            jsonMapper      = store._intern.jsonMapper;
            typeMapper      = (TypeMapper<T>)store._intern.typeStore.GetTypeMapper(typeof(T));
            objectPatcher   = store._intern.objectPatcher;
            tracer          = new Tracer(store._intern.typeCache, store);
            this.store      = store;
            subscription    = null;
            keysBuf         = new List<TKey>();
            autoIncrement   = EntitySet<TKey,T>.EntityKeyTMap.autoIncrement;
        }
    }
}
