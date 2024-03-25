// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    public static class MonitorStoreGen
    {
        // -------------------------------------- input: C# --------------------------------------
        
        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            var options     = new NativeTypeOptions(typeof(MonitorStore));
            var generator   = TypescriptGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/MonitorStore");
        }
        
        /// C# -> GraphQL
        [Test]
        public static void CS_GraphQL () {
            var typeSchema  = NativeTypeSchema.Create(typeof(MonitorStore));
            var generator   = new Generator(typeSchema, ".graphql");
            GraphQLGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/GraphQL/MonitorStore");
        }
        
        /// C# -> Markdown / Mermaid Class Diagram
        [Test]
        public static void CS_Markdown () {
            var typeSchema  = NativeTypeSchema.Create(typeof(MonitorStore));
            var generator   = new Generator(typeSchema, ".md");
            MarkdownGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Markdown/MonitorStore");
        }
    }
}