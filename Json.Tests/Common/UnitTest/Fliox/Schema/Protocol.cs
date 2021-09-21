// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Schema;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    /// <summary>
    /// The sync models define a protocol. The generated Typescript are useful for client applications.
    /// JSON Schema files are not generated as the Graph host / server validate protocol messages by code.  
    /// </summary>
    public static class ProtocolGen
    {
        private static readonly Type[] ProtocolTypes        = { typeof(ProtocolMessage) };

        // -------------------------------------- input: C# --------------------------------------
        
        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            var options     = new NativeTypeOptions(ProtocolTypes);
            var generator   = TypescriptGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/Protocol");
        }
        
        /// C# -> JSON Schema
        // [Test]
        public static void CS_JSON () {
            var options     = new NativeTypeOptions(ProtocolTypes);
            var generator   = JsonSchemaGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/JSON/Protocol");
        }
        
        /// C# -> C#
        // [Test]
        public static void CS_CS () {
            var options     = new NativeTypeOptions(ProtocolTypes) {
                replacements = new[]{new Replace("Friflo.Json.")}
            };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/C#/Protocol");
        }
        
        /// C# -> Kotlin
        [Test]
        public static void CS_Kotlin () {
            var options     = new NativeTypeOptions(ProtocolTypes) {
                replacements = new [] {
                    new Replace("Friflo.Json.Fliox.",                        "Fliox."),
                    new Replace("Friflo.Json.Tests.Common.UnitTest.Fliox",   "Fliox") }
            };
            var generator = KotlinGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Kotlin/src/main/kotlin/Protocol");
        }
        
        /// C# -> JTD
        [Test]
        public static void CS_JTD () {
            var options     = new NativeTypeOptions(ProtocolTypes);
            var generator   = JsonTypeDefinition.Generate(options, "Fliox");
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/JTD/", false);
        }
    }
}