// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Tests.Common.UnitTest.Flow.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class Sync
    {
        private static readonly Type[] SyncTypes        = { typeof(DatabaseMessage) };

        // ---------------------------------------- C# ----------------------------------------
        
        /// <summary>C# -> Typescript - protocol: <see cref="DatabaseMessage"/></summary>
        [Test]
        public static void CS_Typescript () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = TypescriptGenerator.Generate(typeStore, SyncTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/Sync");
        }
        
        /// <summary>C# -> JTD - protocol: <see cref="DatabaseMessage"/></summary>
        [Test]
        public static void CS_JTD () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonTypeDefinition.Generate(typeStore, SyncTypes, "Sync");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JTD/", false);
        }
    }
}