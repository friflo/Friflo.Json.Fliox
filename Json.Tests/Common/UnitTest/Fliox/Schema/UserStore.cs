// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Graph;
using Friflo.Json.Fliox.Schema;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.UserAuth;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    public static class UserStoreGen
    {
        private static readonly Type[] UserStoreTypes      = EntityStore.GetEntityTypes<UserStore>();

        // -------------------------------------- input: C# --------------------------------------
        
        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            var options     = new NativeTypeOptions(typeof(UserStore));
            var generator   = TypescriptGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/UserStore");
        }
        
        /// C# -> JSON Schema
        [Test, Order(1)]
        public static void CS_JSON () {
            var options     = new NativeTypeOptions(typeof(UserStore)) { separateTypes = UserStoreTypes };
            var generator   = JsonSchemaGenerator.Generate(options);
            generator.WriteFiles(JsonSchemaFolder);
        }
        
        /// C# -> C#
        [Test]
        public static void CS_CS () {
            var options     = new NativeTypeOptions(UserStoreTypes) {
                replacements = new [] { new Replace("Friflo.Json.Fliox.", "UserStore2.") }
            };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/C#/UserStore2");
        }
        
        /// C# -> Kotlin
        [Test]
        public static void CS_Kotlin () {
            var options     = new NativeTypeOptions(UserStoreTypes) {
                replacements = new [] { new Replace("Friflo.Json.Fliox.", "UserStore.") }
            };
            var generator = KotlinGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Kotlin/src/main/kotlin/UserStore");
        }
        
        /// C# -> JTD
        [Test]
        public static void CS_JTD () {
            var options     = new NativeTypeOptions(UserStoreTypes);
            var generator = JsonTypeDefinition.Generate(options, "UserStore");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/JTD/", false);
        }
        
        // ---------------------------------- input: JSON Schema ----------------------------------
        
        static readonly string JsonSchemaFolder = CommonUtils.GetBasePath() + "assets~/Schema/JSON/UserStore";
        
        
        /// JSON Schema -> Typescript
        [Test]
        public static void JSON_Typescript () {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas);
            var options     = new JsonTypeOptions(schema);
            var generator   = TypescriptGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema-Loop/Typescript/UserStore");
        }
        
        /// JSON Schema -> JSON Schema
        [Test, Order(2)]
        public static void JSON_JSON () {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = SchemaTest.TypesAsJsonTypes (UserStoreTypes, "Friflo.Json.Fliox.UserAuth.");
            var typeDefs    = schema.TypesAsTypeDefs(jsonTypes);
            var options     = new JsonTypeOptions(schema) { separateTypes = typeDefs };
            var generator   = JsonSchemaGenerator.Generate(options);
            
            var loopFolder = CommonUtils.GetBasePath() + "assets~/Schema-Loop/JSON/UserStore";
            generator.WriteFiles(loopFolder, false);
            SchemaTest.AssertFoldersAreEqual(JsonSchemaFolder, loopFolder);
        }
    }
}