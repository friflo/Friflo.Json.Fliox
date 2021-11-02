// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
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
            using (var _            = UtilsInternal.SharedPools) // for LeakTestsFixture
            using (var pools        = Pools.Create())
            using (var database     = new MemoryDatabase())
            using (var hub          = new FlioxHub(database))
            {
                AssertEntityIdTests (hub, pools);
            }
        }
            
        private static void AssertEntityIdTests(FlioxHub hub, IPools pools) {
            Exception e;
            e = Throws<InvalidTypeException>(() => {
                _ = new TypeMismatchStore(hub, pools) { ClientId = "store"};
            });
            AreEqual("key Type mismatch. String (IntEntity.id) != Int64 (EntitySet<Int64,IntEntity>)", e.Message);
            
            e = Throws<InvalidTypeException>(() => {
                _ = new TypeMismatchStore2(hub, pools) { ClientId =  "store"};
            });
            AreEqual("key Type mismatch. String (IntEntity2.id) != Int64 (EntitySet<Int64,IntEntity2>)", e.Message);
            
            e = Throws<InvalidOperationException>(() => {
                _ = new UnsupportedKeyTypeStore(hub, pools) { ClientId = "store"};
            });
            AreEqual("unsupported TKey Type: EntitySet<Char,CharEntity> id", e.Message);
            
            e = Throws<InvalidTypeException>(() => {
                _ = new InvalidMemberStore(hub, pools) { ClientId = "store"};
            });
            AreEqual("Invalid member: StringEntity.entityRef - Ref<Int32, StringEntity> != EntitySet<String, StringEntity>", e.Message);
        }
    }

    // --------
    public class IntEntity {
        public  string      id;
    }
    
    public class TypeMismatchStore : FlioxClient {
        public  readonly    EntitySet <long, IntEntity> intEntities;

        public TypeMismatchStore(FlioxHub hub, IPools pools) : base(hub, pools) { }
    }
    
    // --------
    public class IntEntity2 {
        public  string      id;
    }
    
    public class TypeMismatchStore2 : FlioxClient {
        // ReSharper disable once UnassignedReadonlyField
        public  readonly    EntitySet <long, IntEntity2> intEntities; // test without assignment

        public TypeMismatchStore2(FlioxHub hub, IPools pools) : base(hub, pools) { }
    }

    // --------
    public class CharEntity {
        public  char        id;
    }
    
    public class UnsupportedKeyTypeStore : FlioxClient {
        public  readonly    EntitySet <char, CharEntity>    charEntities;

        public UnsupportedKeyTypeStore(FlioxHub hub, IPools pools) : base(hub, pools) { }
    }
    
    // --------
    public class StringEntity {
        public  string                  id;
        public  Ref<int, StringEntity>  entityRef;
    }
    
    public class InvalidMemberStore : FlioxClient {
        public  readonly    EntitySet <string,    StringEntity> stringEntities;

        public InvalidMemberStore(FlioxHub hub, IPools pools) : base(hub, pools) { }
    }
}