// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class GenerateTypescript
    {
        [Test]
        public static void TestTypescriptUserStore () {
            var types = new [] { typeof(Role), typeof(UserCredential), typeof(UserCredential) };
            var schema = new SchemaGenerator(types);
            EntityStore.AddTypeMappers(schema.typeStore);
            var generator = schema.Typescript();
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/UserStore");
        }
        
        [Test]
        public static void TestTypescriptSync () {
            var types = new [] { typeof(DatabaseMessage) };
            var schema = new SchemaGenerator(types);
            EntityStore.AddTypeMappers(schema.typeStore);
            var generator = schema.Typescript();
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/Sync");
        }
    }
}