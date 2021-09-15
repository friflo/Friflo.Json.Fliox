// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Schema;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    /// <summary>
    /// The sync models define a protocol. The generated Typescript are useful for client applications.
    /// JSON Schema files are not generated as the Graph host / server validate protocol messages by code.  
    /// </summary>
    public static class SyncGen
    {
        private static readonly Type[] SyncTypes        = { typeof(DatabaseMessage) };

        // -------------------------------------- input: C# --------------------------------------
        
        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            var options     = new NativeTypeOptions(SyncTypes);
            var generator   = TypescriptGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/Sync");
        }
        
        /// C# -> JSON Schema
        // [Test]
        public static void CS_JSON () {
            var options     = new NativeTypeOptions(SyncTypes);
            var generator   = JsonSchemaGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/JSON/Sync");
        }
        
        /// C# -> C#
        // [Test]
        public static void CS_CS () {
            var options     = new NativeTypeOptions(SyncTypes) {
                replacements = new[]{new Replace("Friflo.Json.")}
            };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/C#/Sync");
        }
        
        /// C# -> Kotlin
        [Test]
        public static void CS_Kotlin () {
            var options     = new NativeTypeOptions(SyncTypes) {
                replacements = new [] {
                    new Replace("Friflo.Json.Fliox.",                        "Sync."),
                    new Replace("Friflo.Json.Tests.Common.UnitTest.Fliox",   "Sync") }
            };
            var generator = KotlinGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Kotlin/src/main/kotlin/Sync");
        }
        
        /// C# -> JTD
        [Test]
        public static void CS_JTD () {
            var options     = new NativeTypeOptions(SyncTypes);
            var generator   = JsonTypeDefinition.Generate(options, "Sync");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/JTD/", false);
        }
    }
}