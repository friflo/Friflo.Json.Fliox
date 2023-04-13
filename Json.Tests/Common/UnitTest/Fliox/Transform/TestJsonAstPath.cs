// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
        public void TestJsonAstPathScalar() {
            var astReader   = new JsonAstReader();
            var json        = "{\"a\":1, \"b\":2.5, \"c\":\"abc\", \"d\":true, \"e\":false, \"f\":null, \"g\":{\"g1\":11}, \"h\":[12]}";
            var ast         = CreateAst(astReader, json);
            {
                var found   = ast.GetPathScalar("a", out var value);
                IsTrue(found);
                AreEqual(1, value.AsLong());
            } {
                var found   = ast.GetPathScalar("b", out var value);
                IsTrue(found);
                AreEqual(2.5, value.AsDouble());
            } {
                var found   = ast.GetPathScalar("c", out var value);
                IsTrue(found);
                AreEqual("abc", value.AsString());
            } {
                var found   = ast.GetPathScalar("d", out var value);
                IsTrue(found);
                AreEqual(true, value.AsBool());
            } {
                var found   = ast.GetPathScalar("e", out var value);
                IsTrue(found);
                AreEqual(false, value.AsBool());
            } {
                var found   = ast.GetPathScalar("f", out var value);
                IsTrue(found);
                IsTrue(value.IsNull);
            } { 
                var found   = ast.GetPathScalar("g", out var value);
                IsTrue(found);
                AreEqual(ScalarType.Object, value.type);

                var firstChild = value.GetFirstAstChild();
                AreEqual(firstChild, value.GetFirstAstChild());
                var nodeScalar = ast.GetNodeScalar(firstChild);
                AreEqual(11, nodeScalar.AsLong());
            } {
                var found   = ast.GetPathScalar("h", out var value);
                IsTrue(found);
                AreEqual(ScalarType.Array, value.type);

                var firstChild = value.GetFirstAstChild();
                AreEqual(10, firstChild);
                var nodeScalar = ast.GetNodeScalar(firstChild);
                AreEqual(12, nodeScalar.AsLong());

                found   = ast.GetPathScalar("x", out value);
                IsFalse(found);
            }
        }
        
        [Test]
        public void TestJsonAstPathScalarNested() {
            var astReader   = new JsonAstReader();
            var json        = "{\"a\": {\"a1\": 11, \"a2\": 12}}";
            var ast         = CreateAst(astReader, json);
            {
                var found   = ast.GetPathScalar("a.a1", out var value);
                IsTrue(found);
                AreEqual(11, value.AsLong());
            } {
                var found   = ast.GetPathScalar("a.a2", out var value);
                IsTrue(found);
                AreEqual(12, value.AsLong());
            } {
                var found   = ast.GetPathScalar("a", out var value);
                IsTrue(found);
                AreEqual(ScalarType.Object, value.type);
            } {
                var found   = ast.GetPathScalar("a.x", out _);
                IsFalse(found);
            }
        }
        
        private static JsonAst CreateAst(JsonAstReader reader, string json) {
            var value    =  new JsonValue(json);
            return reader.CreateAst(value);
        }
    }
}