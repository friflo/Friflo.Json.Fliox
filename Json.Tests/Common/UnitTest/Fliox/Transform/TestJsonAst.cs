// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

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
            var json        = writer.WriteAsValue(sample);
            var astReader   = new JsonAstReader();

            var ast = astReader.CreateAst(json); // allocate buffers
            AreEqual(41, ast.NodesCount);
            
            var start = Mem.GetAllocatedBytes();
            for (int n = 0; n < 1; n++) {
                astReader.CreateAst(json);
            }
            var diff = Mem.GetAllocationDiff(start);
            Mem.NoAlloc(diff);
            
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
            var json        = writer.WriteAsValue(sample);
            var astReader   = new JsonAstReader();
            var ast         = astReader.CreateAst(json);
            var astWriter   = new JsonAstWriter();
            astWriter.WriteNullMembers = true;
            var result      = astWriter.WriteAst(ast);
            AreEqual(json.AsString(), result.AsString());
            
            var start = Mem.GetAllocatedBytes();
            for (int n = 0; n < 1; n++) {
                astWriter.WriteAstBytes(ast);
            }
            var diff = Mem.GetAllocationDiff(start);
            Mem.NoAlloc(diff);
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
        }
        
        [Test]
        public void TestJsonTreeObjects() {
            var astReader   = new JsonAstReader();
            {
                var json    =  new JsonValue("{}");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(ObjectStart,   node.type);
                AreEqual(-1,            node.child);
            }
            {
                var json    =  new JsonValue("{\"a\": 10}");
                var ast     = astReader.CreateAst(json);
                AreEqual(2, ast.NodesCount);
                // --- child
                var child1  = ast.GetNode(1);
                AreEqual(ValueNumber,   child1.type);
                AreEqual(-1,            child1.child);
                AreEqual("a",           ast.GetSpanString(child1.key));
                AreEqual("10",          ast.GetSpanString(child1.value));
            }
            {
                var json    =  new JsonValue("{\"array\": [], \"array2\": []}");
                var ast     = astReader.CreateAst(json);
                AreEqual(3, ast.NodesCount);
                // --- child
                var child1  = ast.GetNode(1);
                AreEqual(ArrayStart,    child1.type);
                AreEqual(-1,            child1.child);
                AreEqual("array",       ast.GetSpanString(child1.key));
            }
        }
        
        [Test]
        public void TestJsonTreeArrays() {
            var astReader   = new JsonAstReader();
            {
                var json    =  new JsonValue("[]");
                var ast     = astReader.CreateAst(json);
                AreEqual(1, ast.NodesCount);
                var node    = ast.GetNode(0);
                AreEqual(ArrayStart,    node.type);
                AreEqual(-1,            node.child);
            }
            {
                var json    =  new JsonValue("[11,22]");
                var ast     = astReader.CreateAst(json);
                AreEqual(3, ast.NodesCount);
                // --- child
                var child1  = ast.GetNode(1);
                AreEqual(ValueNumber,   child1.type);
                AreEqual(-1,            child1.child);
                AreEqual("11",          ast.GetSpanString(child1.value));
            }
            {
                var json    =  new JsonValue("[null,null]");
                var ast     = astReader.CreateAst(json);
                AreEqual(3, ast.NodesCount);
                // --- child
                var child1  = ast.GetNode(1);
                AreEqual(ValueNull,     child1.type);
                AreEqual(-1,            child1.child);
                AreEqual("null",        ast.GetSpanString(child1.value));
            }
            {
                var json    =  new JsonValue("[true,false]");
                var ast     = astReader.CreateAst(json);
                AreEqual(3, ast.NodesCount);
                // --- child
                var child1  = ast.GetNode(1);
                AreEqual(ValueBool,     child1.type);
                AreEqual(-1,            child1.child);
                AreEqual("true",        ast.GetSpanString(child1.value));
            }
            {
                var json    =  new JsonValue("[\"abc\",\"xyz\"]");
                var ast     = astReader.CreateAst(json);
                AreEqual(3, ast.NodesCount);
                // --- child
                var child1  = ast.GetNode(1);
                AreEqual(ValueString,   child1.type);
                AreEqual(-1,            child1.child);
                AreEqual("abc",         ast.GetSpanString(child1.value));
            }
            {
                var json    =  new JsonValue("[{},{}]");
                var ast     = astReader.CreateAst(json);
                AreEqual(3, ast.NodesCount);
                // --- child
                var child1  = ast.GetNode(1);
                AreEqual(ObjectStart,   child1.type);
                AreEqual(-1,            child1.child);
            }
            {
                var json    =  new JsonValue("[[],[]]");
                var ast     = astReader.CreateAst(json);
                AreEqual(3, ast.NodesCount);
                // --- child
                var child1  = ast.GetNode(1);
                AreEqual(ArrayStart,    child1.type);
                AreEqual(-1,            child1.child);
            }
        }
        
        [Test]
        public void TestJsonTreeErrors() {
            var astReader   = new JsonAstReader();
            {
                var json    = new JsonValue("{\"key\": a");
                var ast     = astReader.CreateAst(json);
                AreEqual("unexpected character while reading value. Found: a path: 'key' at position: 9", ast.Error);
            }
            {
                var json    = new JsonValue("{}b");
                var ast     = astReader.CreateAst(json);
                AreEqual("Expected EOF path: '(root)' at position: 3", ast.Error);
            }
            {
                var json    = new JsonValue("[c]");
                var ast     = astReader.CreateAst(json);
                AreEqual("unexpected character while reading value. Found: c path: '[0]' at position: 2", ast.Error);
            }
            {
                var json    = new JsonValue("d");
                var ast     = astReader.CreateAst(json);
                AreEqual("unexpected character while reading value. Found: d path: '(root)' at position: 1", ast.Error);
            }
        }
    }
}