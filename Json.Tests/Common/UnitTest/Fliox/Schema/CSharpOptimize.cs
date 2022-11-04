// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    [TestFixture, Ignore("WIP")]
    public static class CSharpOptimize
    {
        /// C# -> Optimize - Assembly: Friflo.Json.Tests
        [Test]
        public static void CS_Optimize_JsonTests () {
            var typeSchema  = NativeTypeSchema.Create(typeof(PocStore));
            var generator   = new Generator(typeSchema, ".cs");
            CSharpOptimizeGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "Gen");
        }
        
        /// C# -> Optimize - Assembly: Friflo.Fliox.Hub
        [Test]
        public static void CS_Optimize_FlioxHub () {
            var typeSchema  = NativeTypeSchema.Create(typeof(ProtocolMessage));
            var generator   = new Generator(typeSchema, ".cs");
            CSharpOptimizeGenerator.Generate(generator);
            var basePath = Path.GetFullPath(CommonUtils.GetBasePath() + "../Json/"); 
            generator.WriteFiles(basePath + "Fliox.Hub/Gen");
        }
    }
}