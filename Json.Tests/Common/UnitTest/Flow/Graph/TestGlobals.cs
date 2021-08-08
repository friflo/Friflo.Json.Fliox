// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public static class TestGlobals {
        public static TypeStore typeStore;
        
        public static void Init() {
            SyncTypeStore.Init();
            // LeakTestsFixture requires to register all types used by TypeStore before leak tracking starts 
            typeStore = new TypeStore();
            typeStore.GetTypeMapper(typeof(TestMessage));
            
            // create all TypeMappers required by PocStore model classes before leak tracking of LeakTestsFixture starts.
            EntityStore.AddTypeMatchers(typeStore);
            var entityTypes = EntityStore.GetEntityTypes<PocStore>();
            typeStore.AddMappers(entityTypes);
            typeStore.GetTypeMapper(typeof(Entity)); // todo necessary?
        }
        
        public static void Dispose() {
            typeStore.Dispose();
            typeStore = null;
            SyncTypeStore.Dispose();
        }
    }
}