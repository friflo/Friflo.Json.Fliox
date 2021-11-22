// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;
using Friflo.Json.Tests.Common.Utils;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public static class TestGlobals {
        public static readonly string PocStoreFolder = CommonUtils.GetBasePath() + "assets~/DB/PocStore";
            
        
        public static SharedEnv Shared { get; private set; }
        
        public static void Init() {
            SharedTypeStore.Init();
            // LeakTestsFixture requires to register all types used by TypeStore before leak tracking starts
            Shared        = new SharedEnv();
            RegisterTypeMatcher(Shared.TypeStore);
            RegisterTypeMatcher(JsonSerializer.DebugTypeStore);
        }
        
        private static void RegisterTypeMatcher(TypeStore typeStore) {
            // create all TypeMappers required by PocStore model classes before leak tracking of LeakTestsFixture starts.
            typeStore.GetTypeMapper(typeof(PocStore));
            typeStore.GetTypeMapper(typeof(PocEntity)); // todo necessary?
            typeStore.GetTypeMapper(typeof(SimpleStore));
            //
            typeStore.GetTypeMapper(typeof(MonitorStore));
            
            SharedTypeStore.AddHostTypes(typeStore);
        }
        
        public static void Dispose() {
            Shared.Dispose();
            Shared = null;
            SharedTypeStore.Dispose();
        }
    }
}