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
    /// <summary>
    /// The sync models define a protocol. The generated Typescript are useful for client applications.
    /// JSON Schema files are not generated as the Graph host / server validate protocol messages by code.  
    /// </summary>
    public static class SyncSchema
    {
        private static readonly Type[] SyncTypes        = { typeof(DatabaseMessage) };

        // -------------------------------------- input: C# --------------------------------------
        
        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var options = new NativeTypeOptions(typeStore, SyncTypes);
            var generator = TypescriptGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/Sync");
        }
        
        /// C# -> C#
        // [Test]
        public static void CS_CS () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var options = new NativeTypeOptions(typeStore, SyncTypes) {
                replacements = new[]{new Replace("Friflo.Json.")}
            };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/CSharp/Sync");
        }
        
        /// C# -> Kotlin
        // [Test]
        public static void CS_Kotlin () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var options = new NativeTypeOptions(typeStore, SyncTypes) {
                replacements = new [] {
                    new Replace("Friflo.Json.Flow.",                        "Sync."),
                    new Replace("Friflo.Json.Tests.Common.UnitTest.Flow",   "Sync") }
            };
            var generator = KotlinGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Kotlin/src/main/kotlin/Sync");
        }
        
        /// C# -> JTD
        [Test]
        public static void CS_JTD () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var options = new NativeTypeOptions(typeStore, SyncTypes);
            var generator = JsonTypeDefinition.Generate(options, "Sync");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JTD/", false);
        }
    }
}