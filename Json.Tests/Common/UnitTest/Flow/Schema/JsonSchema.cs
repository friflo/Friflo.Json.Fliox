// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
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
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var options = new NativeTypeOptions(typeStore, JsonFlowSchemaTypes);
            var generator = TypescriptGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/JsonSchema");
        }
        
        /// C# -> JSON Schema
        [Test, Order(1)]
        public static void CS_JSON () {
            var typeStore   = EntityStore.AddTypeMatchers(new TypeStore());
            var options     = new NativeTypeOptions(typeStore, typeof(JsonSchema));
            var generator   = JsonSchemaGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/JSON/JsonSchema");
        }
        
        /// C# -> C#
        // [Test]
        public static void CS_CS () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var options = new NativeTypeOptions(typeStore, JsonFlowSchemaTypes) {
                replacements = new[]{new Replace("Friflo.Json.")}
            };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/C#/JsonSchema");
        }
    }
}