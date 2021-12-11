// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Parser;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public static class TestEvaluator
    {
        [Test]
        public static void TestJsonEvaluator() {
            using (var eval = new JsonEvaluator()) {
                AssertJsonEvaluator(eval);
            }
        }
        
        private static void AssertJsonEvaluator(JsonEvaluator eval) {
            var     json    = "{}";
            string  error;
            {
                var result = Eval ("1 == 1", json, eval, out _);
                AreEqual(true, result);
            } {
                Eval ("Abs('abc') >= 1", json, eval, out error);
                AreEqual("expect numeric operand. was: 'abc' in Abs('abc')", error);
            } {
                Eval ("1 < Abs(null)", json, eval, out error);
                AreEqual("expect numeric operand. was: null in Abs(null)", error);
            }
        }
        
        private static object Eval(string operation, string json, JsonEvaluator eval, out string error) {
            var op      = QueryParser.Parse(operation, out _);
            var lambda  = new JsonLambda(op);
            var value   = new JsonValue(json);
            var result  = eval.Eval(value, lambda, out error);
            return result;
        } 
    }
}