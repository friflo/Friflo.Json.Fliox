// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform.Tree;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Burst.JsonEvent;

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
            var astReader   = new JsonAstReader();

            var ast = astReader.CreateAst(json); // allocate buffers
            AreEqual(41, ast.NodesCount);
            
            var start = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < 1; n++) {
                astReader.CreateAst(json);
            }
            var dif = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual(0, dif);
            
            var count = TraverseNode(ast, 0);
            AreEqual(41, count);
            
            for (int n = 0; n < 1; n++) {
                TraverseNode(ast, 0);
            }

            for (int n = 0; n < 1; n++) {
                astReader.Test(json);
                // astParser.CreateAst(json);
            }
            

        }
        
        private static  int TraverseNode(JsonAst ast, int index) {
            var nodes = ast.Nodes;
            int count = 0;
            while (index != - 1) {
                count++;
                var node = nodes[index];
                if (node.child != -1) {
                    count += TraverseNode(ast, node.child);
                }
                index = node.Next;
            }
            return count;
        }
        
        [Test]
        public void TestJsonTreeWriter() {
            var sample      = new SampleIL();
            var writer      = new ObjectWriter(new TypeStore());
            var jsonArray   = writer.WriteAsArray(sample);
            var json        = new JsonValue(jsonArray);
            var astReader   = new JsonAstReader();
            var ast         = astReader.CreateAst(json);
            var astWriter   = new JsonAstWriter();
            var result      = astWriter.WriteAst(ast);
            AreEqual(json.AsString(), result.AsString());
        }
        
        [Test]
        public void TestJsonTreePrimitives() {
            var astReader   = new JsonAstReader();
            {   // --- string
                var json    =  new JsonValue("\"abc\"");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(ValueString,   node.type);
                AreEqual("abc",         ast.GetSpanString(node.value));
                AreEqual(-1,            node.child);
            }
            {   // --- number
                var json    =  new JsonValue("123");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(ValueNumber,   node.type);
                AreEqual("123",         ast.GetSpanString(node.value));
                AreEqual(-1,            node.child);

            }
            {   // --- bool
                var json    =  new JsonValue("true");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(ValueBool,     node.type);
                AreEqual("true",        ast.GetSpanString(node.value));
                AreEqual(-1,            node.child);

            }
            {   // --- null
                var json    =  new JsonValue("null");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(ValueNull,     node.type);
                AreEqual("null",        ast.GetSpanString(node.value));
                AreEqual(-1,            node.child);

            }
            {   // --- object
                var json    =  new JsonValue("{}");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(ObjectStart,   node.type);
                AreEqual(-1,            node.child);

            }
            {   // --- array
                var json    =  new JsonValue("[]");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(ArrayStart,    node.type);
                AreEqual(-1,            node.child);
            }
        }
    }
}