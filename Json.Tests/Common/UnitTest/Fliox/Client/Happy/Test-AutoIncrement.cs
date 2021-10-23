// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public class TestAutoIncrement
    {
        [Test] public async Task AutoIncrement () { await AssertAutoIncrement(); }
        
        private static async Task AssertAutoIncrement() {
            using (var _            = UtilsInternal.SharedPools) // for LeakTestsFixture
            using (var typeStore    = new TypeStore())
            using (var database     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/EntityIdStore"))
            using (var hub          = new FlioxHub(database)) {
                await AssertAutoIncrement (hub, typeStore);
            }
        }
        
        private static async Task AssertAutoIncrement(FlioxHub hub, TypeStore typeStore)
        {
            using (var store = new EntityIdStore(hub, typeStore, "autoIncrement")) {
                var delete = store.intEntitiesAuto.DeleteAll();
                await store.SyncTasks();
                IsTrue(delete.Success);
                
                var intEntity = new AutoIntEntity();
                var create  = store.intEntitiesAuto.Create(intEntity);
                var reserve = store.intEntitiesAuto.ReserveKeys(10);
                
                await store.SyncTasks();
                
                IsTrue  (reserve.Success);
                AreEqual(10, reserve.Count);
                var keys = reserve.Keys;
                var diff = keys[9] - keys[0];
                AreEqual(9, diff);
                
                var reserve2 = store.intEntitiesAuto.ReserveKeys(20);
                await store.SyncTasks();
                
                var keys2 = reserve2.Keys;
                AreEqual(20, keys2[0] - keys[0]);

                IsTrue(create.Success);
            }
        }
    }
}