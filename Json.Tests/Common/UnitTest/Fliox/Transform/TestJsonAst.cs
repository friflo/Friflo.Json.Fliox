// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform.Tree;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper;
using NUnit.Framework;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestJsonAst
    {
        [Test]
        public void TestCreateJsonTree() {
            var sample      = new SampleIL();
            var writer      = new ObjectWriter(new TypeStore());
            writer.Pretty   = true;
            var jsonArray   = writer.WriteAsArray(sample);
            var json        = new JsonValue(jsonArray);
            
            var jsonAstParser   = new JsonAstSerializer();
            
            for (int n = 0; n < 1; n++) {
                var jsonAst         = jsonAstParser.CreateAst(json);
            }
        }
    }
}