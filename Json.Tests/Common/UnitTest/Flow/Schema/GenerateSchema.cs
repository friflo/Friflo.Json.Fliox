// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class GenerateSchema
    {
        private static readonly Type[] UserStoreTypes   = { typeof(Role), typeof(UserCredential), typeof(UserPermission) };
        private static readonly Type[] SyncTypes        = { typeof(DatabaseMessage) };

        [Test]
        public static void TypescriptUserStore () {
            var schema = new SchemaGenerator(UserStoreTypes);
            EntityStore.AddTypeMappers(schema.typeStore);
            var generator = schema.Typescript();
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/UserStore");
        }
        
        [Test]
        public static void JsonSchemaUserStore () {
            var schema = new SchemaGenerator(UserStoreTypes);
            EntityStore.AddTypeMappers(schema.typeStore);
            var generator = schema.JsonSchema();
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
        }
        
        [Test]
        public static void TypescriptSync () {
            var schema = new SchemaGenerator(SyncTypes);
            EntityStore.AddTypeMappers(schema.typeStore);
            var generator = schema.Typescript();
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/Sync");
        }
    }
}