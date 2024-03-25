// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
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
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var env          = new SharedEnv())
            using (var database     = new MemoryDatabase(TestGlobals.DB))
            using (var hub          = new FlioxHub(database, env))
            {
                AssertEntityIdTests (hub);
            }
        }
            
        private static void AssertEntityIdTests(FlioxHub hub) {
            Exception e;
            e = Throws<InvalidTypeException>(() => {
                _ = new TypeMismatchStore(hub) { ClientId = "store"};
            });
            AreEqual("key Type mismatch. String (IntEntity.id) != Int64 (EntitySet<Int64,IntEntity>). Used by: TypeMismatchStore.intEntities", e.Message);
            AreEqual(4, StackDepth(e));
            
            e = Throws<InvalidTypeException>(() => {
                _ = new TypeMismatchStore2(hub) { ClientId =  "store"};
            });
            AreEqual("key Type mismatch. String (IntEntity2.id) != Int64 (EntitySet<Int64,IntEntity2>). Used by: TypeMismatchStore2.intEntities", e.Message);
            AreEqual(4, StackDepth(e));
            
            e = Throws<InvalidTypeException>(() => {
                _ = new UnsupportedKeyTypeStore(hub) { ClientId = "store"};
            });
            AreEqual("unsupported TKey Type: EntitySet<Char,CharEntity> id. Used by: UnsupportedKeyTypeStore.charEntities", e.Message);
            AreEqual(4, StackDepth(e));
            
            e = Throws<InvalidTypeException>(() => {
                _ = new InvalidMemberStore(hub) { ClientId = "store"};
            });
            AreEqual("[Relation('stringEntities')] at StringEntity.entityRef invalid type. Expect: String. Used by: InvalidMemberStore.stringEntities", e.Message);
            AreEqual(4, StackDepth(e));
            
            e = Throws<InvalidTypeException>(() => {
                _ = new ContainerNotFoundStore(hub) { ClientId = "store"};
            });
            AreEqual("[Relation('unknown')] at ContainerNotFound.reference not found. Used by: ContainerNotFoundStore.entities", e.Message);
            AreEqual(4, StackDepth(e));
            
            e = Throws<InvalidTypeException>(() => {
                _ = new MissingKeyStore(hub) { ClientId = "store"};
            });
            AreEqual("Missing primary [Key] field/property in entity type: MissingKeyEntity. Used by: MissingKeyStore.entities", e.Message);
            AreEqual(4, StackDepth(e));
            
            e = Throws<InvalidTypeException>(() => {
                _ = new ReadOnlyKeyStore(hub) { ClientId = "store"};
            });
            AreEqual("entity [Key] property ReadOnlyKeyEntity.id requires { get; set; }. Used by: ReadOnlyKeyStore.entities", e.Message);
            AreEqual(4, StackDepth(e));
        }
        
        ///<summary>Ensure Exception is throw in <see cref="FlioxClient"/> constructor to reduce noise of exception</summary>
        private static int StackDepth(Exception e) {
            return Regex.Matches(e.StackTrace!, "   at ").Count;
        }
        
    }

    // --------
    public class IntEntity {
        public  string      id;
    }
    
    public class TypeMismatchStore : FlioxClient {
        public  readonly    EntitySet <long, IntEntity> intEntities;

        public TypeMismatchStore(FlioxHub hub) : base(hub) { }
    }
    
    // --------
    public class IntEntity2 {
        public  string      id;
    }
    
    public class TypeMismatchStore2 : FlioxClient {
        // ReSharper disable once UnassignedReadonlyField
        public  readonly    EntitySet <long, IntEntity2> intEntities; // test without assignment

        public TypeMismatchStore2(FlioxHub hub) : base(hub) { }
    }

    // --------
    public class CharEntity {
        public  char        id;
    }
    
    public class UnsupportedKeyTypeStore : FlioxClient {
        public  readonly    EntitySet <char, CharEntity>    charEntities;

        public UnsupportedKeyTypeStore(FlioxHub hub) : base(hub) { }
    }
    
    // --------
    public class StringEntity {
        public  string      id;
        [Relation(nameof(InvalidMemberStore.stringEntities))]
        public  int         entityRef;
    }
    
    public class InvalidMemberStore : FlioxClient {
        public  readonly    EntitySet <string,    StringEntity> stringEntities;

        public InvalidMemberStore(FlioxHub hub) : base(hub) { }
    }
    
    // --------
    public class ContainerNotFound {
        public  string      id;
        [Relation("unknown")]
        public  int         reference;
    }
    
    public class ContainerNotFoundStore : FlioxClient {
        public  readonly    EntitySet <string, ContainerNotFound> entities;

        public ContainerNotFoundStore(FlioxHub hub) : base(hub) { }
    }
    
    // --------
    public class MissingKeyEntity {
        public  string      Id;
    }
    
    public class MissingKeyStore : FlioxClient {
        public  readonly    EntitySet <string,    MissingKeyEntity> entities;

        public MissingKeyStore(FlioxHub hub) : base(hub) { }
    }
    
    public class ReadOnlyKeyEntity {
        public  string      id { get; }
    }
    
    public class ReadOnlyKeyStore : FlioxClient {
        public  readonly    EntitySet <string,    ReadOnlyKeyEntity> entities;

        public ReadOnlyKeyStore(FlioxHub hub) : base(hub) { }
    }
}