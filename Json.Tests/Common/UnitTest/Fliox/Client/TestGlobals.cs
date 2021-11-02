// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Monitor;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;
using Friflo.Json.Tests.Common.Utils;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public static class TestGlobals {
        public static readonly string PocStoreFolder = CommonUtils.GetBasePath() + "assets~/DB/PocStore";
            
        private static TypeStore typeStore;
        
        public static Pools Pools;
        
        public static void Init() {
            HostTypeStore.Init();
            // LeakTestsFixture requires to register all types used by TypeStore before leak tracking starts
            typeStore = new TypeStore();
            Pools = UtilsInternal.CreatePools(() => typeStore);
            RegisterTypeMatcher(typeStore);
            RegisterTypeMatcher(JsonDebug.DebugTypeStore);
        }
        
        private static void RegisterTypeMatcher(TypeStore typeStore) {
            // create all TypeMappers required by PocStore model classes before leak tracking of LeakTestsFixture starts.
            typeStore.GetTypeMapper(typeof(PocStore));
            typeStore.GetTypeMapper(typeof(PocEntity)); // todo necessary?
            typeStore.GetTypeMapper(typeof(SimpleStore));
            //
            typeStore.GetTypeMapper(typeof(MonitorStore));
        }
        
        public static void Dispose() {
            Pools.Dispose();
            Pools = null;
            typeStore.Dispose();
            typeStore = null;
            HostTypeStore.Dispose();
        }
    }
}