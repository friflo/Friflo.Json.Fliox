// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Graph;
using Friflo.Json.Fliox.Schema;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Graph;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    public static class PocStoreGen
    {
        private static readonly Type[]  PocStoreTypes       = EntityStore.GetEntityTypes<PocStore>();
        
        // -------------------------------------- input: C# --------------------------------------

        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            // Use code generator directly
            var schema      = new NativeTypeSchema(typeof(PocStore));
            var generator   = new Generator(schema, ".ts", new[]{new Replace("Friflo.Json.Tests.Common.")});
            TypescriptGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/PocStore");
        }
        
        /// C# -> JSON Schema
        [Test, Order(1)]
        public static void CS_JsonSchema () {
            // Use code generator directly
            var schema      = new NativeTypeSchema(typeof(PocStore));
            var sepTypes    = schema.TypesAsTypeDefs(PocStoreTypes);
            var generator   = new Generator(schema, ".json", new[]{new Replace("Friflo.Json.Tests.Common.")}, sepTypes);
            JsonSchemaGenerator.Generate(generator);
            generator.WriteFiles(JsonSchemaFolder);
        }
        
        /// C# -> C#
        [Test]
        public static void CS_CS () {
            var options     = new NativeTypeOptions(PocStoreTypes) {
                replacements = new [] {
                    new Replace("Friflo.Json.Fliox.",                        "PocStore2."),
                    new Replace("Friflo.Json.Tests.Common.UnitTest.Fliox",   "PocStore2") }
            };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/C#/PocStore2");
        }
        
        /// C# -> Kotlin
        [Test]
        public static void CS_Kotlin () {
            var options     = new NativeTypeOptions(PocStoreTypes) {
                replacements = new [] {
                    new Replace("Friflo.Json.Fliox.",                        "PocStore."),
                    new Replace("Friflo.Json.Tests.Common.UnitTest.Fliox",   "PocStore") }
            };
            var generator = KotlinGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Kotlin/src/main/kotlin/PocStore");
        }
        
        /// C# -> JTD
        [Test]
        public static void CS_JTD () {
            var options     = new NativeTypeOptions(PocStoreTypes);
            var generator   = JsonTypeDefinition.Generate(options, "PocStore");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/JTD/", false);
        }
        
        // ---------------------------------- input: JSON Schema ----------------------------------
        
        static readonly string JsonSchemaFolder = CommonUtils.GetBasePath() + "assets~/Schema/JSON/PocStore";
        
        /// JSON Schema -> JSON Schema
        [Test, Order(2)]
        public static void JSON_JSON () {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = SchemaTest.TypesAsJsonTypes (PocStoreTypes, "UnitTest.Fliox.Graph.");
            var typeDefs    = schema.TypesAsTypeDefs(jsonTypes);
            var options     = new JsonTypeOptions(schema) { separateTypes = typeDefs };
            var generator   = JsonSchemaGenerator.Generate(options);
            var loopFolder = CommonUtils.GetBasePath() + "assets~/Schema-Loop/JSON/PocStore";
            generator.WriteFiles(loopFolder);
            SchemaTest.AssertFoldersAreEqual(JsonSchemaFolder, loopFolder);
        }
        
        /// JSON Schema -> Typescript
        [Test]
        public static void JSON_Typescript () {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas);
            var options     = new JsonTypeOptions(schema);
            var generator   = TypescriptGenerator.Generate(options);

            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema-Loop/Typescript/PocStore");

        }
    }
}