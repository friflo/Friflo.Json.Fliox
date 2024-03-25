// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    public static class PocStoreGen
    {
        private static readonly Type[]      PocStoreTypes   = FlioxClient.GetEntityTypes(typeof(PocStore));
        private static readonly Replace[]   Replacements    =  {
            new Replace("Friflo.Json.Tests.Common.")
        };
        
        // -------------------------------------- input: C# --------------------------------------

        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            // Use code generator directly
            var schema      = NativeTypeSchema.Create(typeof(PocStore));
            var generator   = new Generator(schema, ".d.ts", Replacements);
            TypescriptGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/PocStore");
        }
        
        /// C# -> HTML
        [Test]
        public static void CS_HTML () {
            // Use code generator directly
            var schema      = NativeTypeSchema.Create(typeof(PocStore));
            var generator   = new Generator(schema, ".html");
            HtmlGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Html/PocStore");
        }
        
        /// C# -> JSON Schema / OpenAPI
        [Test, Order(1)]
        public static void CS_JsonSchema () {
            // Use code generator directly
            var schema      = NativeTypeSchema.Create(typeof(PocStore));
            var sepTypes    = schema.TypesAsTypeDefs(PocStoreTypes);
            var generator   = new Generator(schema, ".json", Replacements, sepTypes);
            JsonSchemaGenerator.Generate(generator);
            generator.WriteFiles(JsonSchemaFolder);
        }
        
        /// C# -> C#
        [Test]
        public static void CS_CS () {
            // requires individual replacements. Otherwise the generated CS classes will result in name clashes, as
            // their namespace / class names are equal to the original ones.
            var replacements = new [] {
                new Replace("Friflo.Json.Tests.Common.UnitTest.Fliox",  "PocStore2"),
                new Replace("Friflo.Json.Fliox",                        "PocStore2")
            };
            var options     = new NativeTypeOptions(typeof(PocStore)) { replacements = replacements };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/C#/PocStore2");
        }
        
        /// C# -> Kotlin
        [Test]
        public static void CS_Kotlin () {
            var options     = new NativeTypeOptions(typeof(PocStore)) { replacements = Replacements };
            var generator = KotlinGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Kotlin/src/main/kotlin/PocStore");
        }
        
        /// C# -> JTD
        [Test]
        public static void CS_JTD () {
            var options     = new NativeTypeOptions(typeof(PocStore));
            var generator   = JsonTypeDefinition.Generate(options, "PocStore");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/JTD/", false);
        }
        
        /// C# -> GraphQL
        [Test]
        public static void CS_GraphQL () {
            var typeSchema  = NativeTypeSchema.Create(typeof(PocStore));
            var generator   = new Generator(typeSchema, ".graphql");
            GraphQLGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/GraphQL/PocStore");
        }
        
        /// C# -> Markdown / Mermaid Class Diagram
        [Test]
        public static void CS_Markdown () {
            var typeSchema  = NativeTypeSchema.Create(typeof(PocStore));
            var generator   = new Generator(typeSchema, ".md");
            MarkdownGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Markdown/PocStore");
        }
        
        // ---------------------------------- input: JSON Schema ----------------------------------
        
        static readonly string JsonSchemaFolder = CommonUtils.GetBasePath() + "assets~/Schema/JSON/PocStore";
        
        /// JSON Schema -> JSON Schema
        [Test, Order(2)]
        public static void JSON_JSON () {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas, "./UnitTest.Fliox.Client.json#/definitions/PocStore");
            var jsonTypes   = SchemaTest.TypesAsJsonTypes (PocStoreTypes, "UnitTest.Fliox.Client.");
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