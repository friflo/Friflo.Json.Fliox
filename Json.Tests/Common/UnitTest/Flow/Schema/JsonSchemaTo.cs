// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.UnitTest.Flow.Graph;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class JsonSchemaTo
    {
        /// <summary>JSON Schema -> Typescript - model: <see cref="UserStore"/></summary>
        [Test]
        public static void Typescript_UserStore () {
            var schemas     = JsonTypeSchema.FromFolder(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = JsonTypesFromTypes (CSharpTo.UserStoreTypes, "Friflo.Json.Flow.UserAuth.");
            var generator   = TypescriptGenerator.Generate(schema, jsonTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema-Loop/Typescript/UserStore");
        }
        
        /// <summary>JSON Schema -> Typescript - model: <see cref="PocStore"/></summary>
        [Test]
        public static void Typescript_PocStore () {
            var schemas     = JsonTypeSchema.FromFolder(CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore");
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = JsonTypesFromTypes (CSharpTo.PocStoreTypes, "UnitTest.Flow.Graph.");
            var generator   = TypescriptGenerator.Generate(schema, jsonTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema-Loop/Typescript/PocStore");
        }
        
        /// <summary>JSON Schema -> JSON Schema - model: <see cref="UserStore"/></summary>
        [Test]
        public static void JSON_UserStore () {
            var schemas     = JsonTypeSchema.FromFolder(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = JsonTypesFromTypes (CSharpTo.UserStoreTypes, "Friflo.Json.Flow.UserAuth.");
            var typeDefs    = schema.TypesAsTypeDefs(jsonTypes);
            var generator   = JsonSchemaGenerator.Generate(schema, jsonTypes, typeDefs);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema-Loop/JSON/UserStore");
        }
        
        /// <summary>JSON Schema -> JSON Schema - model: <see cref="PocStore"/></summary>
        [Test]
        public static void JSON_PocStore () {
            var schemas     = JsonTypeSchema.FromFolder(CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore");
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = JsonTypesFromTypes (CSharpTo.PocStoreTypes, "UnitTest.Flow.Graph.");
            var typeDefs    = schema.TypesAsTypeDefs(jsonTypes);
            var generator   = JsonSchemaGenerator.Generate(schema, jsonTypes, typeDefs);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema-Loop/JSON/PocStore");
        }

        private static List<string> JsonTypesFromTypes(ICollection<Type> types, string package) {
            var list = new List<string>();
            foreach (var type in types) {
                list.Add($"./{package}{type.Name}.json#/definitions/{type.Name}");
            }
            return list;
        }
    }
}