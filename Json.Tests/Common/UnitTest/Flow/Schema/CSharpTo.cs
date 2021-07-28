// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.Native;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.UnitTest.Flow.Graph;
using Friflo.Json.Tests.Common.UnitTest.Flow.Schema.lab;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class CSharpTo
    {
        public static readonly Type[] UserStoreTypes   = { typeof(Role), typeof(UserCredential), typeof(UserPermission) };
        public static readonly Type[] SyncTypes        = { typeof(DatabaseMessage) };
        public static readonly Type[] PocStoreTypes    = { typeof(Order), typeof(Customer), typeof(Article), typeof(Producer), typeof(Employee), typeof(TestType) };

        /// <summary>C# -> Typescript - model: <see cref="UserStore"/></summary>
        [Test]
        public static void Typescript_UserStore () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = TypescriptGenerator.Generate(typeStore, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/UserStore");
        }
        
        /// <summary>C# -> JSON Schema - model: <see cref="UserStore"/></summary>
        [Test]
        public static void JsonSchema_UserStore () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonSchemaGenerator.Generate(typeStore, UserStoreTypes, null, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
        }
        
        /// <summary>C# -> JTD - model: <see cref="UserStore"/></summary>
        [Test]
        public static void JTD_UserStore () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonTypeDefinition.Generate(typeStore, UserStoreTypes, "UserStore", null, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JTD/", false);
        }
        
        /// <summary>C# -> Typescript - protocol: <see cref="DatabaseMessage"/></summary>
        [Test]
        public static void Typescript_Sync () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = TypescriptGenerator.Generate(typeStore, SyncTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/Sync");
        }
        
        /// <summary>C# -> JTD - protocol: <see cref="DatabaseMessage"/></summary>
        [Test]
        public static void JTD_Sync () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonTypeDefinition.Generate(typeStore, SyncTypes, "Sync");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JTD/", false);
        }
        
        /// <summary>C# -> JTD - model: <see cref="PocStore"/></summary>
        [Test]
        public static void JTD_PocStore () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonTypeDefinition.Generate(typeStore, PocStoreTypes, "PocStore");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JTD/", false);
        }
        
        /// <summary>C# -> Typescript - model: <see cref="PocStore"/></summary>
        [Test]
        public static void Typescript_PocStore () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(PocStoreTypes);
            var schema      = new NativeTypeSchema(typeStore);
            var generator   = new Generator(schema, ".ts", new[]{"Friflo.Json.Tests.Common."});
            var _           = new TypescriptGenerator(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/PocStore");
        }
        
        /// <summary>C# -> JSON Schema - model: <see cref="PocStore"/></summary>
        [Test]
        public static void JsonSchema_PocStore () {
            // Use code generator directly
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(PocStoreTypes);
            var schema      = new NativeTypeSchema(typeStore, PocStoreTypes);
            var generator   = new Generator(schema, ".json", new[]{"Friflo.Json.Tests.Common."});
            var _           = new JsonSchemaGenerator(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore");
        }
    }
}