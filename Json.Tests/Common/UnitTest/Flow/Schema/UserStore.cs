// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.UnitTest.Flow.Schema.lab;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class JsonSchemaTo
    {
        private static readonly Type[] UserStoreTypes   = { typeof(Role), typeof(UserCredential), typeof(UserPermission) };

        // -------------------------------------- input: C# --------------------------------------
        
        /// <summary>C# -> Typescript - model: <see cref="UserStore"/></summary>
        [Test]
        public static void CS_Typescript () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = TypescriptGenerator.Generate(typeStore, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/UserStore");
        }
        
        /// <summary>C# -> JSON Schema - model: <see cref="UserStore"/></summary>
        [Test, Order(1)]
        public static void CS_JSON () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonSchemaGenerator.Generate(typeStore, UserStoreTypes, null, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
        }
        
        /// <summary>C# -> JTD - model: <see cref="UserStore"/></summary>
        [Test]
        public static void CS_JTD () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonTypeDefinition.Generate(typeStore, UserStoreTypes, "UserStore", null, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JTD/", false);
        }
        
        // ---------------------------------- input: JSON Schema ----------------------------------
        
        /// <summary>JSON Schema -> Typescript - model: <see cref="UserStore"/></summary>
        [Test, Order(2)]
        public static void JSON_Typescript () {
            var schemas     = JsonTypeSchema.FromFolder(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = SchemaTest.JsonTypesFromTypes (UserStoreTypes, "Friflo.Json.Flow.UserAuth.");
            var generator   = TypescriptGenerator.Generate(schema, jsonTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema-Loop/Typescript/UserStore");
        }
        
        /// <summary>JSON Schema -> JSON Schema - model: <see cref="UserStore"/></summary>
        [Test]
        public static void JSON_JSON () {
            var schemas     = JsonTypeSchema.FromFolder(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = SchemaTest.JsonTypesFromTypes (UserStoreTypes, "Friflo.Json.Flow.UserAuth.");
            var typeDefs    = schema.TypesAsTypeDefs(jsonTypes);
            var generator   = JsonSchemaGenerator.Generate(schema, jsonTypes, typeDefs);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema-Loop/JSON/UserStore");
        }
    }
}