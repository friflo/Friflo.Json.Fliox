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
        public static void TestEval() {
            using (var eval = new JsonEvaluator()) {
                AssertEval(eval);
            }
        }

        private const string  Json    = @"{""strVal"": ""abc"", ""intVal"": 42}";

        
        private static void AssertEval(JsonEvaluator eval) {
            string  error;
            {
                var result = Eval ("1 == 1", Json, eval, out _);
                AreEqual(true, result);
            }
            // --- arithmetic functions
            {
                Eval ("Abs('abc') >= 1", Json, eval, out error);
                AreEqual("expect numeric operand. was: 'abc' in Abs('abc')", error);
            } {
                Eval ("1 < Abs(null)", Json, eval, out error);
                AreEqual("expect numeric operand. was: null in Abs(null)", error);
            }
            // --- arithmetic operators
            {
                Eval ("1 * 'abc'", Json, eval, out error);
                AreEqual("expect two numeric operands. left: 1, right: 'abc'", error);
            } {
                Eval ("2 + 'abc'", Json, eval, out error);
                AreEqual("expect two numeric operands. left: 2, right: 'abc'", error);
            } {
                Eval ("'abc' - 3", Json, eval, out error);
                AreEqual("expect two numeric operands. left: 'abc', right: 3", error);
            } {
                Eval ("'abc' / 4", Json, eval, out error);
                AreEqual("expect two numeric operands. left: 'abc', right: 4", error);
            }
            // --- string functions
            {
                Eval (".strVal.Contains(.intVal)", Json, eval, out error);
                AreEqual("expect two string operands. left: 'abc', right: 42", error);
            } {
                Eval (".strVal.StartsWith(.intVal)", Json, eval, out error);
                AreEqual("expect two string operands. left: 'abc', right: 42", error);
            } {
                Eval (".strVal.EndsWith(.intVal)", Json, eval, out error);
                AreEqual("expect two string operands. left: 'abc', right: 42", error);
            } {
                Eval (".intVal.EndsWith('abc')", Json, eval, out error);
                AreEqual("expect two string operands. left: 42, right: 'abc'", error);
            } /* {
                Eval (".foo.EndsWith(.bar)", json, eval, out error);
                AreEqual("expect two string operands. left: 42, right: 'abc'", error);
            } */
        }
        
        [Test]
        public static void TestFilter() {
            using (var eval = new JsonEvaluator()) {
                AssertFilter(eval);
            }
        }
        
        private static void AssertFilter(JsonEvaluator eval) {
            {
                var result = Filter (".foo.EndsWith('abc')", Json, eval, out _);
                IsFalse(result);
            } {
                var result = Filter (".strVal.EndsWith('abc')", Json, eval, out _);
                IsTrue(result);
            }
        }
        
        private static object Eval(string operation, string json, JsonEvaluator eval, out string error) {
            var op      = QueryParser.Parse(operation, out error);
            if (error != null)
                return null;
            var lambda  = new JsonLambda(op);
            var value   = new JsonValue(json);
            var result  = eval.Eval(value, lambda, out error);
            return result;
        }
        
        private static bool Filter(string operation, string json, JsonEvaluator eval, out string error) {
            var op      = (FilterOperation)QueryParser.Parse(operation, out error);
            if (error != null)
                return false;
            var filter  = new JsonFilter(op);
            var value   = new JsonValue(json);
            var result  = eval.Filter(value, filter, out error);
            return result;
        }
    }
}