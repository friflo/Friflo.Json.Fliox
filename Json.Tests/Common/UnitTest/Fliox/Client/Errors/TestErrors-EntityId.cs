// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable UnassignedReadonlyField
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Errors
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
            
            e = Throws<InvalidTypeException>(() => {
                _ = new TypeMismatchStore2(database, typeStore, "store");
            });
            AreEqual("key Type mismatch. String (IntEntity2.id) != Int64 (EntitySet<Int64,IntEntity2>)", e.Message);
            
            e = Throws<InvalidOperationException>(() => {
                _ = new UnsupportedKeyTypeStore(database, typeStore, "store");
            });
            AreEqual("unsupported TKey Type: EntitySet<Char,CharEntity> id", e.Message);
            
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
        public  readonly    EntitySet <long, IntEntity> intEntities;

        public TypeMismatchStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {}
    }
    
    // --------
    public class IntEntity2 {
        public  string      id;
    }
    
    public class TypeMismatchStore2 : EntityStore {
        // ReSharper disable once UnassignedReadonlyField
        public  readonly    EntitySet <long, IntEntity2> intEntities; // test without assignment

        public TypeMismatchStore2(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {}
    }

    // --------
    public class CharEntity {
        public  char        id;
    }
    
    public class UnsupportedKeyTypeStore : EntityStore {
        public  readonly    EntitySet <char, CharEntity>    charEntities;

        public UnsupportedKeyTypeStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {}
    }
    
    // --------
    public class StringEntity {
        public  string                  id;
        public  Ref<int, StringEntity>  entityRef;
    }
    
    public class InvalidMemberStore : EntityStore {
        public  readonly    EntitySet <string,    StringEntity> stringEntities;

        public InvalidMemberStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {}
    }
}