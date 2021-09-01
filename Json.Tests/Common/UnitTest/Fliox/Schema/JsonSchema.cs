// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Schema;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    /// <summary>
    /// The sync models define a protocol. The generated Typescript are useful for client applications.
    /// JSON Schema files are not generated as the Graph host / server validate protocol messages by code.  
    /// </summary>
    public static class JsonSchemaGen
    {
        private static readonly Type[] JsonFlowSchemaTypes        = { typeof(JsonSchema) };

        // -------------------------------------- input: C# --------------------------------------
        
        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            var options     = new NativeTypeOptions(JsonFlowSchemaTypes);
            var generator   = TypescriptGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/JsonSchema");
        }
        
        /// C# -> JSON Schema
        [Test, Order(1)]
        public static void CS_JSON () {
            var options     = new NativeTypeOptions(typeof(JsonSchema));
            var generator   = JsonSchemaGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/JSON/JsonSchema");
        }
        
        /// C# -> C#
        // [Test]
        public static void CS_CS () {
            var options     = new NativeTypeOptions(JsonFlowSchemaTypes) {
                replacements = new[]{new Replace("Friflo.Json.")}
            };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/C#/JsonSchema");
        }
    }
}