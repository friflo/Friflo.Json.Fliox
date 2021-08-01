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
    public static class PocStoreSchema
    {
        private static readonly Type[] PocStoreTypes    = { typeof(Order), typeof(Customer), typeof(Article), typeof(Producer), typeof(Employee), typeof(TestType) };
        
        // -------------------------------------- input: C# --------------------------------------

        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(PocStoreTypes);
            var schema      = new NativeTypeSchema(typeStore);
            var generator   = new Generator(schema, ".ts", new[]{new Replace("Friflo.Json.Tests.Common.")});
            TypescriptGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/PocStore");
        }
        
        /// C# -> JSON Schema
        [Test, Order(1)]
        public static void CS_JsonSchema () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(PocStoreTypes);
            var schema      = new NativeTypeSchema(typeStore);
            var sepTypes    = schema.TypesAsTypeDefs(PocStoreTypes);
            var generator   = new Generator(schema, ".json", new[]{new Replace("Friflo.Json.Tests.Common.")}, sepTypes);
            JsonSchemaGenerator.Generate(generator);
            generator.WriteFiles(JsonSchemaFolder);
        }
        
        /// C# -> C#
        [Test]
        public static void CS_CS () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var options = new NativeTypeOptions(typeStore, PocStoreTypes) {
                replacements = new [] {
                    new Replace("Friflo.Json.Flow.",                        "PocStore2."),
                    new Replace("Friflo.Json.Tests.Common.UnitTest.Flow",   "PocStore2") }
            };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/CSharp/PocStore2");
        }
        
        /// C# -> Kotlin
        [Test]
        public static void CS_Kotlin () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var options = new NativeTypeOptions(typeStore, PocStoreTypes) {
                replacements = new [] {
                    new Replace("Friflo.Json.Flow.",                        "PocStore."),
                    new Replace("Friflo.Json.Tests.Common.UnitTest.Flow",   "PocStore") }
            };
            var generator = KotlinGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Kotlin/src/main/kotlin/PocStore");
        }
        
        /// C# -> JTD
        [Test]
        public static void CS_JTD () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var options = new NativeTypeOptions(typeStore, PocStoreTypes);
            var generator = JsonTypeDefinition.Generate(options, "PocStore");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JTD/", false);
        }
        
        // ---------------------------------- input: JSON Schema ----------------------------------
        
        static readonly string JsonSchemaFolder = CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore";
        
        /// JSON Schema -> JSON Schema
        [Test, Order(2)]
        public static void JSON_JSON () {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = SchemaTest.JsonTypesFromTypes (PocStoreTypes, "UnitTest.Flow.Graph.");
            var typeDefs    = schema.TypesAsTypeDefs(jsonTypes);
            var options     = new JsonTypeOptions(schema) { separateTypes = typeDefs };
            var generator   = JsonSchemaGenerator.Generate(options);
            var loopFolder = CommonUtils.GetBasePath() + "assets/Schema-Loop/JSON/PocStore";
            generator.WriteFiles(loopFolder);
            SchemaTest.AssertFoldersAreEqual(JsonSchemaFolder, loopFolder);
        }
        
        /// JSON Schema -> Typescript
        [Test]
        public static void JSON_Typescript () {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = SchemaTest.JsonTypesFromTypes (PocStoreTypes, "UnitTest.Flow.Graph.");
            var options     = new JsonTypeOptions(schema);
            var generator   = TypescriptGenerator.Generate(options);

            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema-Loop/Typescript/PocStore");

        }
    }
}