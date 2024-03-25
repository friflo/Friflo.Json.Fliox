// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Tree;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestJsonAstPath
    {
        [Test]
        public void TestJsonAstPathScalarRoot() {
            const string root = "";
            var astReader   = new JsonAstReader();
            {
                var ast     = CreateAst(astReader, "1");
                var found   = ast.GetPathValue(root, out var value);
                IsTrue(found);
                AreEqual(1, value.AsLong());
             } {
                var ast     = CreateAst(astReader, "1.5");
                var found   = ast.GetPathValue(root, out var value);
                IsTrue(found);
                AreEqual(1.5, value.AsDouble());
            } {
                var ast     = CreateAst(astReader, "true");
                var found   = ast.GetPathValue(root, out var value);
                IsTrue(found);
                AreEqual(true, value.AsBool());
            } {
                var ast     = CreateAst(astReader, "false");
                var found   = ast.GetPathValue(root, out var value);
                IsTrue(found);
                AreEqual(false, value.AsBool());
            } {
                var ast     = CreateAst(astReader, "null");
                var found   = ast.GetPathValue(root, out var value);
                IsTrue(found);
                IsTrue(value.IsNull);
            } {
                var ast     = CreateAst(astReader, "\"xyz\"");
                var found   = ast.GetPathValue(root, out var value);
                IsTrue(found);
                AreEqual("xyz", value.AsString());
            } {
                var ast     = CreateAst(astReader, "[11]");
                var found   = ast.GetPathValue(root, out var value);
                IsTrue(found);
                AreEqual(ScalarType.Array, value.type);
                
                var firstChild = value.GetFirstAstChild();
                AreEqual(1, firstChild);
                var nodeScalar = ast.GetNodeValue(firstChild);
                AreEqual(11, nodeScalar.AsLong());
            } {
                var ast     = CreateAst(astReader, "{\"a\":12}");
                var found   = ast.GetPathValue(root, out var value);
                IsTrue(found);
                AreEqual(ScalarType.Object, value.type);
                
                var firstChild = value.GetFirstAstChild();
                AreEqual(1, firstChild);
                var nodeScalar = ast.GetNodeValue(firstChild);
                AreEqual(12, nodeScalar.AsLong());
            }
        }
        
        [Test]
        public void TestJsonAstPathScalarObject() {
            var astReader   = new JsonAstReader();
            // nodes:          0 1        2          3              4           5            6           7      8           9     10            
            var json        = "{\"a\":1, \"b\":2.5, \"c\":\"abc\", \"d\":true, \"e\":false, \"f\":null, \"g\":{\"g1\":11}, \"h\":[12]}";
            var ast         = CreateAst(astReader, json);
            {
                var found   = ast.GetPathValue("a", out var value);
                IsTrue(found);
                AreEqual(1, value.AsLong());
            } {
                var found   = ast.GetPathValue("b", out var value);
                IsTrue(found);
                AreEqual(2.5, value.AsDouble());
            } {
                var found   = ast.GetPathValue("c", out var value);
                IsTrue(found);
                AreEqual("abc", value.AsString());
            } {
                var found   = ast.GetPathValue("d", out var value);
                IsTrue(found);
                AreEqual(true, value.AsBool());
            } {
                var found   = ast.GetPathValue("e", out var value);
                IsTrue(found);
                AreEqual(false, value.AsBool());
            } {
                var found   = ast.GetPathValue("f", out var value);
                IsTrue(found);
                IsTrue(value.IsNull);
            } { 
                var found   = ast.GetPathValue("g", out var value);
                IsTrue(found);
                AreEqual(ScalarType.Object, value.type);

                var firstChild = value.GetFirstAstChild();
                AreEqual(8, firstChild);
                var nodeScalar = ast.GetNodeValue(firstChild);
                AreEqual(11, nodeScalar.AsLong());
            } {
                var found   = ast.GetPathValue("h", out var value);
                IsTrue(found);
                AreEqual(ScalarType.Array, value.type);

                var firstChild = value.GetFirstAstChild();
                AreEqual(10, firstChild);
                var nodeScalar = ast.GetNodeValue(firstChild);
                AreEqual(12, nodeScalar.AsLong());
            } {
                var found   = ast.GetPathValue("x", out var value);
                IsTrue (found);
                IsTrue(value.IsNull);
            }
        }
        
        [Test]
        public void TestJsonAstPathScalarNested() {
            var astReader   = new JsonAstReader();
            var json        = "{\"a\": {\"a1\": 11, \"a2\": 12}, \"b\":1}";
            var ast         = CreateAst(astReader, json);
            {
                var found   = ast.GetPathValue("a.a1", out var value);
                IsTrue(found);
                AreEqual(11, value.AsLong());
            } {
                var found   = ast.GetPathValue("a.a2", out var value);
                IsTrue(found);
                AreEqual(12, value.AsLong());
            } {
                var found   = ast.GetPathValue("a", out var value);
                IsTrue(found);
                AreEqual(ScalarType.Object, value.type);
            } {
                var found   = ast.GetPathValue("a.x", out var value);
                IsTrue(found);
                IsTrue(value.IsNull);
            } {
                var found   = ast.GetPathValue("b.b1", out var value);
                IsTrue(found);
                IsTrue(value.IsNull);
            }
        }
        
        [Test]
        public void TestJsonAstPathPerf() {
            var astReader   = new JsonAstReader();
            var json        = "{\"a\":1, \"b\":2, \"c\":3, \"d\":4, \"e\":5, \"f\":6, \"g\":{\"g1\":8}}";
            CreateAst(astReader, json);
            var ast         = CreateAst(astReader, json);
            var path        = JsonAst.GetPathItems("g.g1");
            var count       = 10; // 10_000_000;
            for (int n = 0; n < count; n++) {
                bool found = ast.GetPathValue(0, path, out _);
                if (!found) throw new InvalidOperationException();
            }
        }
        
        private static JsonAst CreateAst(JsonAstReader reader, string json) {
            var value    =  new JsonValue(json);
            return reader.CreateAst(value);
        }
    }
}