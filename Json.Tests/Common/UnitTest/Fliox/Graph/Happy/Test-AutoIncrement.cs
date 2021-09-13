// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph.Happy
{
    public class TestAutoIncrement
    {
        // [Test] public async Task AutoIncrement () { await AssertAutoIncrement(); }
        
        private static async Task AssertAutoIncrement() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var typeStore    = new TypeStore())
            using (var database     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/AutoIncrement")) {
                await AssertAutoIncrement (database, typeStore);
            }
        }
        
        private static async Task AssertAutoIncrement(EntityDatabase database, TypeStore typeStore)
        {
            using (var store = new EntityIdStore(database, typeStore, "autoIncrement")) {
                var intEntity = new IntEntity();
                var create  = store.intEntities.Create(intEntity);
                
                await store.Sync();
                
                IsTrue(create.Success);
            }
        }
    }
}