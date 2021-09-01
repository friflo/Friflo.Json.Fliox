// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Graph;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Graph;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    public static class EntityIdStoreGen
    {
        private static readonly Type[] EntityIdStoreTypes      = EntityStore.GetEntityTypes<EntityIdStore>();

        // -------------------------------------- input: C# --------------------------------------
        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            // Use code generator directly
            var typeStore   = new TypeStore();
            var schema      = new NativeTypeSchema(typeStore, typeof(EntityIdStore));
            var generator   = new Generator(schema, ".ts", new[]{new Replace("Friflo.Json.Tests.Common.")});
            TypescriptGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/EntityIdStore");
        }
        
        /// C# -> JSON Schema
        [Test, Order(1)]
        public static void CS_JSON () {
            var typeStore   = new TypeStore();
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
            var typeStore   = new TypeStore();
            var options     = new NativeTypeOptions(typeStore, EntityIdStoreTypes) {
                replacements = new [] {new Replace("Friflo.Json.Tests.Common.UnitTest.Fliox", "EntityIdStore2") }
            };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/C#/EntityIdStore2");
        }
        
        /// C# -> Kotlin
        [Test]
        public static void CS_Kotlin () {
            var typeStore   = new TypeStore();
            var options     = new NativeTypeOptions(typeStore, EntityIdStoreTypes) {
                replacements = new [] {
                    new Replace("Friflo.Json.Tests.Common.UnitTest.Fliox",   "EntityIdStore") }
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
            var schema      = new JsonTypeSchema(schemas, "./UnitTest.Fliox.Graph.json#/definitions/EntityIdStore");
            var entityTypes = schema.GetEntityTypes();
            var options     = new JsonTypeOptions(schema) { separateTypes = entityTypes };
            var generator   = JsonSchemaGenerator.Generate(options);
            
            var loopFolder  = CommonUtils.GetBasePath() + "assets~/Schema-Loop/JSON/EntityIdStore";
            generator.WriteFiles(loopFolder, false);
            SchemaTest.AssertFoldersAreEqual(JsonSchemaFolder, loopFolder);
        }
    }
}