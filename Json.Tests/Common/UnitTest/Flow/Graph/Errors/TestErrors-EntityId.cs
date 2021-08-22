// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Tests.Common.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Errors
{
    public class TestErrorEntityId
    {
        [Test]
        public static void EntityIdTypeMismatch() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var typeStore    = new TypeStore())
            using (var database     = new MemoryDatabase()) {
                AssertEntityIdTests (database, typeStore);
            }
        }
            
        private static void AssertEntityIdTests(EntityDatabase database, TypeStore typeStore) {
            var e = Throws<InvalidTypeException>(() => {
                _ = new EntityIdErrorStore(database, typeStore, "guidStore");
            });
            AreEqual("Key Type mismatch. String (IntEntity.id) != Int64 (EntitySet<Int64,IntEntity>)", e.Message);
        }
    }
    
    public class EntityIdErrorStore : EntityStore {
        public  readonly    EntitySet <long,    IntEntity>      intEntities;

        public EntityIdErrorStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            intEntities      = new EntitySet <long,    IntEntity>      (this);
        }
    }

    public class IntEntity {
        public string id;
    }

}