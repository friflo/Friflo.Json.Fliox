// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
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
            Exception e;
            e = Throws<InvalidTypeException>(() => {
                _ = new EntityIdErrorStore(database, typeStore, "store");
            });
            AreEqual("Key Type mismatch. String (IntEntity.id) != Int64 (EntitySet<Int64,IntEntity>)", e.Message);
            
            e = Throws<InvalidOperationException>(() => {
                _ = new EntityIdErrorStore2(database, typeStore, "store");
            });
            AreEqual("unsupported Type for entity key: ByteEntity.id, Type: Byte", e.Message);
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
    
    public class EntityIdErrorStore2 : EntityStore {
        public  readonly    EntitySet <byte,    ByteEntity>      byteEntities;

        public EntityIdErrorStore2(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            byteEntities      = new EntitySet <byte,    ByteEntity>      (this);
        }
    }
    
    public class ByteEntity {
        public byte id;
    }

}