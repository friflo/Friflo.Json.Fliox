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
            {
                var json    = "{\"a\":1, \"b\":2.5, \"c\":\"abc\", \"d\":true, \"e\":false, \"f\":null, \"g\":{}, \"h\":[]}";
                var ast     = CreateAst(astReader, json);
                var found   = ast.GetPathScalar("a", out var value);
                IsTrue(found);
                AreEqual(1, value.AsLong());
                
                found   = ast.GetPathScalar("b", out value);
                IsTrue(found);
                AreEqual(2.5, value.AsDouble());
                
                found   = ast.GetPathScalar("c", out value);
                IsTrue(found);
                AreEqual("abc", value.AsString());
                
                found   = ast.GetPathScalar("d", out value);
                IsTrue(found);
                AreEqual(true, value.AsBool());
                
                found   = ast.GetPathScalar("e", out value);
                IsTrue(found);
                AreEqual(false, value.AsBool());

                found   = ast.GetPathScalar("f", out value);
                IsTrue(found);
                IsTrue(value.IsNull);
                
                found   = ast.GetPathScalar("g", out value);
                IsTrue(found);
                AreEqual(ScalarType.Object, value.type);
                
                found   = ast.GetPathScalar("h", out value);
                IsTrue(found);
                AreEqual(ScalarType.Array, value.type);

                found   = ast.GetPathScalar("x", out value);
                IsFalse(found);
            } {
                var ast     = CreateAst(astReader, "{\"a\": {\"a1\": 11, \"a2\": 12}}");
                var found   = ast.GetPathScalar("a.a1", out var value);
                IsTrue(found);
                AreEqual(11, value.AsLong());
                
                found   = ast.GetPathScalar("a.a2", out value);
                IsTrue(found);
                AreEqual(12, value.AsLong());
                
                found   = ast.GetPathScalar("a", out value);
                IsTrue(found);
                AreEqual(ScalarType.Object, value.type);
                
                found   = ast.GetPathScalar("a.x", out value);
                IsFalse(found);
            }
        }
        
        private static JsonAst CreateAst(JsonAstReader reader, string json) {
            var value    =  new JsonValue(json);
            return reader.CreateAst(value);
        }
    }
}