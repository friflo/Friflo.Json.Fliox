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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable JoinDeclarationAndInitializer
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
                _ = new TypeMismatchStore(database, typeStore, "store");
            });
            AreEqual("key Type mismatch. String (IntEntity.id) != Int64 (EntitySet<Int64,IntEntity>)", e.Message);
            
            e = Throws<InvalidOperationException>(() => {
                _ = new UnsupportedKeyTypeStore(database, typeStore, "store");
            });
            AreEqual("unsupported Type for entity key: ByteEntity.id, Type: Byte", e.Message);
            
            e = Throws<InvalidTypeException>(() => {
                _ = new InvalidMemberStore(database, typeStore, "store");
            });
            AreEqual("Invalid member: StringEntity.entityRef - Ref<Int32, StringEntity> != EntitySet<String, StringEntity>", e.Message);
        }
    }

    // --------
    public class IntEntity {
        public  string      id;
    }
    
    public class TypeMismatchStore : EntityStore {
        public  readonly    EntitySet <long,    IntEntity>      intEntities;

        public TypeMismatchStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            intEntities = new EntitySet <long,    IntEntity>      (this);
        }
    }

    // --------
    public class ByteEntity {
        public  byte        id;
    }
    
    public class UnsupportedKeyTypeStore : EntityStore {
        public  readonly    EntitySet <byte,    ByteEntity>      byteEntities;

        public UnsupportedKeyTypeStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            byteEntities = new EntitySet <byte,    ByteEntity>      (this);
        }
    }
    
    // --------
    public class StringEntity {
        public  string                  id;
        public  Ref<int, StringEntity>  entityRef;
    }
    
    public class InvalidMemberStore : EntityStore {
        public  readonly    EntitySet <string,    StringEntity>      stringEntities;

        public InvalidMemberStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            stringEntities = new EntitySet <string,    StringEntity>      (this);
        }
    }
}