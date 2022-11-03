// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    public static class CSharpOptimize
    {
        /// C# -> Optimize
        // [Test]
        public static void CS_Optimize_PocStore () {
            var typeSchema  = NativeTypeSchema.Create(typeof(PocStore));
            var generator   = new Generator(typeSchema, ".cs");
            CSharpOptimizeGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/C#-Optimize/Tests");
        }
    }
}