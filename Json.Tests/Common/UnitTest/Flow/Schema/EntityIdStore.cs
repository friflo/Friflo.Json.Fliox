// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Flow.Graph;
using Friflo.Json.Tests.Common.UnitTest.Flow.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class EntityIdStoreGen
    {
        private static readonly Type[] EntityIdStoreTypes      = EntityStore.GetEntityTypes<EntityIdStore>();

        // -------------------------------------- input: C# --------------------------------------
        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var schema      = new NativeTypeSchema(typeStore, typeof(EntityIdStore));
            var generator   = new Generator(schema, ".ts", new[]{new Replace("Friflo.Json.Tests.Common.")});
            TypescriptGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/EntityIdStore");
        }
        
        /// C# -> JSON Schema
        [Test, Order(1)]
        public static void CS_JSON () {
            var typeStore   = EntityStore.AddTypeMatchers(new TypeStore());
            var options     = new NativeTypeOptions(typeStore, typeof(EntityIdStore)) {
                separateTypes = EntityIdStoreTypes,
                replacements = new [] {new Replace("Friflo.Json.Tests.Common.")}
            };
            var generator   = JsonSchemaGenerator.Generate(options);
            generator.WriteFiles(JsonSchemaFolder);
        }
        
        /// C# -> C#
        [Test]
        public static void CS_CS () {
            var typeStore   = EntityStore.AddTypeMatchers(new TypeStore());
            var options     = new NativeTypeOptions(typeStore, EntityIdStoreTypes) {
                replacements = new [] {new Replace("Friflo.Json.Tests.Common.UnitTest.Flow", "EntityIdStore2") }
            };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/C#/EntityIdStore2");
        }
        
        /// C# -> Kotlin
        [Test]
        public static void CS_Kotlin () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var options = new NativeTypeOptions(typeStore, EntityIdStoreTypes) {
                replacements = new [] {
                    new Replace("Friflo.Json.Tests.Common.UnitTest.Flow",   "EntityIdStore") }
            };
            var generator = KotlinGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Kotlin/src/main/kotlin/EntityIdStore");
        }

        
        // ---------------------------------- input: JSON Schema ----------------------------------
        
        static readonly string JsonSchemaFolder = CommonUtils.GetBasePath() + "assets~/Schema/JSON/EntityIdStore";
        
        /// JSON Schema -> JSON Schema
        [Test, Order(2)]
        public static void JSON_JSON () {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas, "./UnitTest.Flow.Graph.json#/definitions/EntityIdStore");
            var entityTypes = schema.GetEntityTypes();
            var options     = new JsonTypeOptions(schema) { separateTypes = entityTypes };
            var generator   = JsonSchemaGenerator.Generate(options);
            
            var loopFolder  = CommonUtils.GetBasePath() + "assets~/Schema-Loop/JSON/EntityIdStore";
            generator.WriteFiles(loopFolder, false);
            SchemaTest.AssertFoldersAreEqual(JsonSchemaFolder, loopFolder);
        }
    }
}