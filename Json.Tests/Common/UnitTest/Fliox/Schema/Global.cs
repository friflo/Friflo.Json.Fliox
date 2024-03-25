// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

public class GlobalClient : FlioxClient
{
    // --- containers
    public  readonly    EntitySet <long, GlobalJob>   jobs;
    
    // --- commands
    /// <summary>Delete all jobs marked as completed / not completed</summary>
    public CommandTask<int>  ClearCompletedJobs (bool completed) => send.Command<bool,int>(completed);

    public GlobalClient(FlioxHub hub, string dbName = null) : base (hub, dbName) { }
}

// ---------------------------------- entity models ----------------------------------
public class GlobalJob
{
    [Key]       public  long        id { get; set; }
    ///<summary> short job title / name </summary>
    [Required]  public  string      title;
    public  bool?       completed;
    public  DateTime?   created;
    public  string      description;
}

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    public static class GlobalGen
    {
        private static readonly Type[]      GlobalTypes   = FlioxClient.GetEntityTypes(typeof(GlobalClient));
        
        // -------------------------------------- input: C# --------------------------------------

        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            // Use code generator directly
            var schema      = NativeTypeSchema.Create(typeof(GlobalClient));
            var generator   = new Generator(schema, ".d.ts");
            TypescriptGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/GlobalClient");
        }
        
        /// C# -> HTML
        [Test]
        public static void CS_HTML () {
            // Use code generator directly
            var schema      = NativeTypeSchema.Create(typeof(GlobalClient));
            var generator   = new Generator(schema, ".html");
            HtmlGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Html/GlobalClient");
        }
        
        /// C# -> JSON Schema / OpenAPI
        [Test, Order(1)]
        public static void CS_JsonSchema () {
            // Use code generator directly
            var schema      = NativeTypeSchema.Create(typeof(GlobalClient));
            var sepTypes    = schema.TypesAsTypeDefs(GlobalTypes);
            var generator   = new Generator(schema, ".json", null, sepTypes);
            JsonSchemaGenerator.Generate(generator);
            generator.WriteFiles(JsonSchemaFolder);
        }
        
        /// C# -> Kotlin
        [Test]
        public static void CS_Kotlin () {
            var options     = new NativeTypeOptions(typeof(GlobalClient));
            var generator = KotlinGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Kotlin/src/main/kotlin/GlobalClient");
        }
        
        /// C# -> JTD
        [Test]
        public static void CS_JTD () {
            var options     = new NativeTypeOptions(typeof(GlobalClient));
            var generator   = JsonTypeDefinition.Generate(options, "GlobalClient");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/JTD/", false);
        }
        
        /// C# -> GraphQL
        [Test]
        public static void CS_GraphQL () {
            var typeSchema  = NativeTypeSchema.Create(typeof(GlobalClient));
            var generator   = new Generator(typeSchema, ".graphql");
            GraphQLGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/GraphQL/GlobalClient");
        }
        
        /// C# -> Markdown / Mermaid Class Diagram
        [Test]
        public static void CS_Markdown () {
            var typeSchema  = NativeTypeSchema.Create(typeof(GlobalClient));
            var generator   = new Generator(typeSchema, ".md");
            MarkdownGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Markdown/GlobalClient");
        }
        
        // ---------------------------------- input: JSON Schema ----------------------------------
        
        static readonly string JsonSchemaFolder = CommonUtils.GetBasePath() + "assets~/Schema/JSON/GlobalClient";
        
        /// JSON Schema -> JSON Schema
        [Test, Order(2)]
        public static void JSON_JSON () {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = SchemaTest.TypesAsJsonTypes (GlobalTypes, "Default.");
            var typeDefs    = schema.TypesAsTypeDefs(jsonTypes);
            var options     = new JsonTypeOptions(schema) { separateTypes = typeDefs };
            var generator   = JsonSchemaGenerator.Generate(options);
            var loopFolder = CommonUtils.GetBasePath() + "assets~/Schema-Loop/JSON/GlobalClient";
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

            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema-Loop/Typescript/GlobalClient");

        }
    }
}