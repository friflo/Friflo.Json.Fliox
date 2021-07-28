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

        /// <summary>C# -> Typescript - model: <see cref="PocStore"/></summary>
        [Test]
        public static void CS_Typescript () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(PocStoreTypes);
            var schema      = new NativeTypeSchema(typeStore);
            var generator   = new Generator(schema, ".ts", new[]{"Friflo.Json.Tests.Common."});
            TypescriptGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/PocStore");
        }
        
        /// <summary>C# -> JSON Schema - model: <see cref="PocStore"/></summary>
        [Test, Order(1)]
        public static void CS_JsonSchema () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(PocStoreTypes);
            var schema      = new NativeTypeSchema(typeStore);
            var sepTypes    = schema.TypesAsTypeDefs(PocStoreTypes);
            var generator   = new Generator(schema, ".json", new[]{"Friflo.Json.Tests.Common."}, sepTypes);
            JsonSchemaGenerator.Generate(generator);
            generator.WriteFiles(JsonSchemaFolder);
        }
        
        /// <summary>C# -> JTD - model: <see cref="PocStore"/></summary>
        [Test]
        public static void CS_JTD () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonTypeDefinition.Generate(typeStore, PocStoreTypes, "PocStore");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JTD/", false);
        }
        
        // ---------------------------------- input: JSON Schema ----------------------------------
        
        static readonly string JsonSchemaFolder = CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore";
        
        /// <summary>JSON Schema -> JSON Schema - model: <see cref="PocStore"/></summary>
        [Test, Order(2)]
        public static void JSON_JSON () {
            var schemas     = JsonTypeSchema.FromFolder(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = SchemaTest.JsonTypesFromTypes (PocStoreTypes, "UnitTest.Flow.Graph.");
            var typeDefs    = schema.TypesAsTypeDefs(jsonTypes);
            var generator   = JsonSchemaGenerator.Generate(schema, jsonTypes, typeDefs);
            
            var loopFolder = CommonUtils.GetBasePath() + "assets/Schema-Loop/JSON/PocStore";
            generator.WriteFiles(loopFolder);
            SchemaTest.AssertFoldersAreEqual(JsonSchemaFolder, loopFolder);
        }
        
        /// <summary>JSON Schema -> Typescript - model: <see cref="PocStore"/></summary>
        [Test]
        public static void JSON_Typescript () {
            var schemas     = JsonTypeSchema.FromFolder(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = SchemaTest.JsonTypesFromTypes (PocStoreTypes, "UnitTest.Flow.Graph.");
            var generator   = TypescriptGenerator.Generate(schema, jsonTypes);

            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema-Loop/Typescript/PocStore");

        }
    }
}