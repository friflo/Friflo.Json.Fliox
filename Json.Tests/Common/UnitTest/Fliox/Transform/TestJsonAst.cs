// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform.Tree;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestJsonAst
    {
        [Test]
        public void TestJsonTreeCreate() {
            var sample      = new SampleIL();
            var writer      = new ObjectWriter(new TypeStore());
            writer.Pretty   = true;
            var jsonArray   = writer.WriteAsArray(sample);
            var json        = new JsonValue(jsonArray);
            var astParser   = new JsonAstReader();

            var ast = astParser.CreateAst(json); // allocate buffers
            AreEqual(41, ast.NodesCount);
            
            var start = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < 1; n++) {
                astParser.CreateAst(json);
            }
            var dif = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual(0, dif);
            

            for (int n = 0; n < 1; n++) {
                astParser.Test(json);
                // astParser.CreateAst(json);
            }
        }
        
        [Test]
        public void TestJsonTreePrimitives() {
            var astReader   = new JsonAstReader();
            {   // --- string
                var json    =  new JsonValue("\"abc\"");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(JsonEvent.ValueString, node.type);
                AreEqual("abc", ast.GetSpanString(node.value));
            }
            {   // --- number
                var json    =  new JsonValue("123");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(JsonEvent.ValueNumber, node.type);
                AreEqual("123", ast.GetSpanString(node.value));
            }
            {   // --- bool
                var json    =  new JsonValue("true");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(JsonEvent.ValueBool, node.type);
                AreEqual("true", ast.GetSpanString(node.value));
            }
            {   // --- null
                var json    =  new JsonValue("null");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(JsonEvent.ValueNull, node.type);
                AreEqual("null", ast.GetSpanString(node.value));
            }
            {   // --- object
                var json    =  new JsonValue("{}");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(JsonEvent.ObjectStart, node.type);
            }
            {   // --- array
                var json    =  new JsonValue("[]");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(JsonEvent.ArrayStart, node.type);
            }
        }
    }
}