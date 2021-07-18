// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class GenerateTypescript
    {
        [Test] public static void TestTypescript () {
            var generator = new SchemaGenerator();
            var types = new [] { typeof(Role), typeof(UserCredential), typeof(UserCredential) };
            EntityStore.AddTypeMappers(generator.typeStore);
            generator.CreateSchema(types, CommonUtils.GetBasePath() + "assets/Schema/Typescript/PocStore");
        }
    }
}