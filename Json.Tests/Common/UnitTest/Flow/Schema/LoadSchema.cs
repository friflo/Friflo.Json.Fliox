// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class LoadSchema
    {
        [Test]
        public static void Typescript_UserStore () {
            var schema      = JsonTypeSchema.FromFolder(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
            var jsonTypes   = JsonTypesFromTypes (GenerateSchema.UserStoreTypes, "Friflo.Json.Flow.UserAuth.");
            var generator   = TypescriptGenerator.Generate(schema, jsonTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/SchemaLoop/Typescript/UserStore");
        }
        
        [Test]
        public static void Typescript_PocStore () {
            var schema      = JsonTypeSchema.FromFolder(CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore");
            var jsonTypes   = JsonTypesFromTypes (GenerateSchema.PocStoreTypes, "UnitTest.Flow.Graph.");
            var generator   = TypescriptGenerator.Generate(schema, jsonTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/SchemaLoop/Typescript/PocStore");
        }
        
        [Test]
        public static void JSON_UserStore () {
            var schema      = JsonTypeSchema.FromFolder(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
            var jsonTypes   = JsonTypesFromTypes (GenerateSchema.UserStoreTypes, "Friflo.Json.Flow.UserAuth.");
            var generator   = JsonSchemaGenerator.Generate(schema, jsonTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/SchemaLoop/JSON/UserStore");
        }
        
        [Test]
        public static void JSON_PocStore () {
            var schema      = JsonTypeSchema.FromFolder(CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore");
            var jsonTypes   = JsonTypesFromTypes (GenerateSchema.PocStoreTypes, "UnitTest.Flow.Graph.");
            var generator   = JsonSchemaGenerator.Generate(schema, jsonTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/SchemaLoop/JSON/PocStore");
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