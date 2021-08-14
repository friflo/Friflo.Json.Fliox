// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal struct SetIntern<T> where T : class
    {
        internal readonly   TypeMapper<T>       typeMapper;
        internal readonly   ObjectMapper        jsonMapper;
        internal readonly   ObjectPatcher       objectPatcher;
        internal readonly   Tracer              tracer;
        internal readonly   EntityStore         store;
        internal readonly   FieldInfo           idField;
        internal readonly   Func  <T, string>   idPropertyGet;
        internal readonly   Action<T, string>   idPropertySet;

        
        // --- non readonly
        internal            SubscribeChanges    subscription;

        internal SetIntern(EntityStore store) {
            jsonMapper      = store._intern.jsonMapper;
            typeMapper      = (TypeMapper<T>)store._intern.typeStore.GetTypeMapper(typeof(T));
            objectPatcher   = store._intern.objectPatcher;
            tracer          = new Tracer(store._intern.typeCache, store);
            this.store      = store;
            subscription    = null;
            var id          = EntityId.GetEntityId<T>();
            idField         = id.field;
            idPropertyGet   = id.propertyGet;
            idPropertySet   = id.propertySet;
        }
    }
}
