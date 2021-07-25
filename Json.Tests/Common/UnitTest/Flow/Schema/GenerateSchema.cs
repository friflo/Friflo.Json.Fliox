// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.Native;
using Friflo.Json.Flow.Schema.Utils;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.UnitTest.Flow.Graph;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class GenerateSchema
    {
        private static readonly Type[] UserStoreTypes   = { typeof(Role), typeof(UserCredential), typeof(UserPermission) };
        private static readonly Type[] SyncTypes        = { typeof(DatabaseMessage) };
        private static readonly Type[] PocStoreTypes    = { typeof(Order), typeof(Customer), typeof(Article), typeof(Producer), typeof(Employee), typeof(TestType) };

        [Test]
        public static void TypescriptUserStore () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var schema = new GeneratorSchema(typeStore, UserStoreTypes);
            var generator = schema.Typescript(null, null);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/UserStore");
        }
        
        [Test]
        public static void JsonSchemaUserStore () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var schema = new GeneratorSchema(typeStore, UserStoreTypes);
            var generator = schema.JsonSchema(null, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
        }
        
        [Test]
        public static void TypescriptSync () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var schema = new GeneratorSchema(typeStore, SyncTypes);
            var generator = schema.Typescript(null, null);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/Sync");
        }
        
        [Test]
        public static void TypescriptPocStore () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(PocStoreTypes);
            var schema      = new NativeTypeSchema(typeStore, null);
            var generator   = new Generator(schema, new[]{"Friflo.Json.Tests.Common."}, ".ts");
            var typescript  = new Typescript(generator);
            typescript.GenerateSchema();
            typescript.generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/PocStore");
        }
        
        [Test]
        public static void JsonSchemaPocStore () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(PocStoreTypes);
            var schema      = new NativeTypeSchema(typeStore, PocStoreTypes);
            var generator   = new Generator(schema, new[]{"Friflo.Json.Tests.Common."}, ".json");
            var jsonSchema  = new JsonSchema(generator);
            jsonSchema.GenerateSchema();
            jsonSchema.generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore");
        }
        
        [Test]
        public static void TestTopologicalSort() {
            var a = new Item("A");
            var c = new Item("C");
            var f = new Item("F");
            var h = new Item("H");
            var d = new Item("D", a);
            var g = new Item("G", f, h);
            var e = new Item("E", d, g);
            var b = new Item("B", c, e);

            var unsorted = new[] { a, b, c, d, e, f, g, h };

            var sorted = TopologicalSort.Sort(unsorted, x => x.dependencies);
            
            var expect = new[] { a, c, d, f, h, g, e, b};
            AreEqual (expect, sorted);
        }
        
        public class Item {
            private readonly    string  name;
            public  readonly    Item[]  dependencies;

            public override string  ToString() => name;

            public Item(string name, params Item[] dependencies) {
                this.name = name;
                this.dependencies = dependencies;
            }
        }
    }
}