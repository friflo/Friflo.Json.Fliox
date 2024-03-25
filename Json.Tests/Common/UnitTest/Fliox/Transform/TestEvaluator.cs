// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Parser;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public static class TestEvaluator
    {
        private const string  Json    =
@"{
    ""strVal"":  ""abc"",
    ""intVal"":  42,
    ""nullVal"": null,
    ""boolVal"": true,
    ""obj"":     {},
    ""array"":   [1,2,3]
}";

        [Test]
        public static void TestEvalArithmeticFunctions() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // --- error
                {
                    Eval ("o => Abs(o.strVal) >= 1", Json, eval, out error);
                    AreEqual("expect numeric operand. was: 'abc' in Abs(o.strVal)", error);
                }
                // --- success
                {
                    var result = Eval ("o => Abs(-1)", Json, eval, out error);
                    AreEqual(1, result);
                } {
                    var result = Eval ("o => Log(E)", Json, eval, out error);
                    AreEqual(1, result);
                } {
                    var result = Eval ("o => Exp(1)", Json, eval, out error);
                    AreEqual(Math.E, result);
                } {
                    var result = Eval ("o => Sqrt(4)", Json, eval, out error);
                    AreEqual(2, result);
                } {
                    var result = Eval ("o => Floor(2.5)", Json, eval, out error);
                    AreEqual(2, result);
                } {
                    var result = Eval ("o => Ceiling(2.5)", Json, eval, out error);
                    AreEqual(3, result);
                }
                // --- binary arithmetic
                {
                    var result = Eval ("o => 1 + 2", Json, eval, out error);
                    AreEqual(3, result);
                } {
                    var result = Eval ("o => 1 - 2", Json, eval, out error);
                    AreEqual(-1, result);
                } {
                    var result = Eval ("o => 2 * 3", Json, eval, out error);
                    AreEqual(6, result);
                } {
                    var result = Eval ("o => 6 / 2", Json, eval, out error);
                    AreEqual(3, result);
                } {
                    var result = Eval ("o => 7 % 3", Json, eval, out error);
                    AreEqual(1, result);
                }
                // --- null
                {
                    var result = Eval ("o => Abs(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => Log(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => Exp(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => Sqrt(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => Floor(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => Ceiling(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                } /* { todo
                    var result = Eval ("o => -o.nullVal", Json, eval, out error);
                    IsNull(result);
                } */
            }
        }
        
        [Test]
        public static void TestEvalArithmeticOperators() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // --- error
                {
                    Eval ("o => o.intVal * o.strVal", Json, eval, out error);
                    AreEqual("expect numeric operands. left: 42, right: 'abc' in o.intVal * o.strVal", error);
                } {
                    Eval ("o => o.intVal + o.strVal", Json, eval, out error);
                    AreEqual("expect numeric operands. left: 42, right: 'abc' in o.intVal + o.strVal", error);
                } {
                    Eval ("o => o.strVal - o.intVal", Json, eval, out error);
                    AreEqual("expect numeric operands. left: 'abc', right: 42 in o.strVal - o.intVal", error);
                } {
                    Eval ("o => o.strVal / o.intVal", Json, eval, out error);
                    AreEqual("expect numeric operands. left: 'abc', right: 42 in o.strVal / o.intVal", error);
                }
                // --- success
                {
                    var result = Eval ("o => o.intVal * 1", Json, eval, out error);
                    AreEqual(42, result);
                } {
                    var result = Eval ("o => o.intVal + 1", Json, eval, out error);
                    AreEqual(43, result);
                } {
                    var result = Eval ("o => o.intVal - 1", Json, eval, out error);
                    AreEqual(41, result);
                } {
                    var result = Eval ("o => o.intVal / 2", Json, eval, out error);
                    AreEqual(21, result);
                }
                // --- null left
                {
                    var result = Eval ("o => o.nullVal * 1", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => o.nullVal + 1", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => o.nullVal - 1", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => o.nullVal / 2", Json, eval, out error);
                    IsNull(result);
                }
                // --- null right
                {
                    var result = Eval ("o => 1 * o.nullVal", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => 1 + o.nullVal", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => 1 - o.nullVal", Json, eval, out error);
                    IsNull(result);
                } {
                    var result = Eval ("o => 2 / o.nullVal", Json, eval, out error);
                    IsNull(result);
                }
            }
        }
        
        [Test]
        public static void TestEvalString() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // ------ error
                {
                    Eval ("o => o.strVal.Contains(o.intVal)", Json, eval, out error);
                    AreEqual("expect string operands. left: 'abc', right: 42 in o.strVal.Contains(o.intVal)", error);
                } {
                    Eval ("o => o.strVal.StartsWith(o.intVal)", Json, eval, out error);
                    AreEqual("expect string operands. left: 'abc', right: 42 in o.strVal.StartsWith(o.intVal)", error);
                } {
                    Eval ("o => o.strVal.EndsWith(o.intVal)", Json, eval, out error);
                    AreEqual("expect string operands. left: 'abc', right: 42 in o.strVal.EndsWith(o.intVal)", error);
                } {
                    Eval ("o => o.intVal.EndsWith('abc')", Json, eval, out error);
                    AreEqual("expect string operands. left: 42, right: 'abc' in o.intVal.EndsWith('abc')", error);
                }
                // ------ success
                // --- non null & string methods
                {
                    var result = Filter ("o => o.strVal.EndsWith('abc')", Json, eval, out _);
                    IsTrue(result);
                } {
                    // missing member o.foo => o.foo = null
                    var result = Filter ("o => o.foo.EndsWith('abc')", Json, eval, out _);
                    IsFalse(result);
                } {
                    // missing member o.foo => o.foo = null
                    var result = Filter ("o => o.strVal.EndsWith(o.foo)", Json, eval, out _);
                    IsFalse(result);
                } {
                    var result = Eval ("o => o.strVal.Length()", Json, eval, out _);
                    AreEqual(3, result);
                }
                // --- null left & string methods
                {
                    var result = Eval ("o => o.nullVal.Contains('abc')", Json, eval, out _);
                    IsNull(result);
                } {
                    var result = Eval ("o => o.nullVal.EndsWith('abc')", Json, eval, out _);
                    IsNull(result);
                } {
                    var result = Eval ("o => o.nullVal.StartsWith('abc')", Json, eval, out _);
                    IsNull(result);
                } {
                    var result = Eval ("o => o.nullVal.Length()", Json, eval, out _);
                    IsNull(result);
                }
                // --- null right & string methods
                {
                    var result = Eval ("o => 'abc'.Contains(o.nullVal)", Json, eval, out _);
                    IsNull(result);
                } {
                    var result = Eval ("o => 'abc'.EndsWith(o.nullVal)", Json, eval, out _);
                    IsNull(result);
                } {
                    var result = Eval ("o => 'abc'.StartsWith(o.nullVal)", Json, eval, out _);
                    IsNull(result);
                }
            }
        }
        
        [Test]
        public static void TestEvalNull() {
            using (var eval = new JsonEvaluator()) {
                // --- null compare: == !=
                {
                    var result = Filter ("o => null == null", Json, eval, out _);
                    IsTrue(result);
                } {
                    var result = Filter ("o => null != null", Json, eval, out _);
                    IsFalse(result);
                }
                // --- string property set - compare: == !=
                {
                    var result = Filter ("o => o.strVal == null", Json, eval, out _);
                    IsFalse(result);
                } {
                    var result = Filter ("o => o.strVal != null", Json, eval, out _);
                    IsTrue(result);
                } {
                    var result = Filter ("o => null == o.strVal", Json, eval, out _);
                    IsFalse(result);
                } {
                    var result = Filter ("o => null != o.strVal", Json, eval, out _);
                    IsTrue(result);
                }
                // --- string property null - compare: == !=
                {
                    var result = Filter ("o => o.nullVal == null", Json, eval, out _);
                    IsTrue(result);
                } {
                    var result = Filter ("o => o.nullVal != null", Json, eval, out _);
                    IsFalse(result);
                } {
                    var result = Filter ("o => null == o.nullVal", Json, eval, out _);
                    IsTrue(result);
                } {
                    var result = Filter ("o => null != o.nullVal", Json, eval, out _);
                    IsFalse(result);
                }
            }
        }

        [Test]
        public static void TestEvalEquality() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // --- error
                {
                    Eval ("o => o.strVal == 1", Json, eval, out error);
                    AreEqual("incompatible operands: 'abc' == 1 in o.strVal == 1", error);
                } {
                    Eval ("o => o.strVal == true", Json, eval, out error);
                    AreEqual("incompatible operands: 'abc' == true in o.strVal == true", error);
                } {
                    Eval ("o => true == o.strVal", Json, eval, out error);
                    AreEqual("incompatible operands: true == 'abc' in true == o.strVal", error);
                } {
                    Eval ("o => 1 == o.strVal", Json, eval, out error);
                    AreEqual("incompatible operands: 1 == 'abc' in 1 == o.strVal", error);
                }
                // --- error: object / array
                {
                    Eval ("o => o.obj == 'abc'", Json, eval, out error);
                    AreEqual("invalid operand: (object) == 'abc' in o.obj == 'abc'", error);
                } {
                    Eval ("o => o.array == 'abc'", Json, eval, out error);
                    AreEqual("invalid operand: (array) == 'abc' in o.array == 'abc'", error);
                } {
                    Eval ("o => 'abc' == o.obj", Json, eval, out error);
                    AreEqual("invalid operand: 'abc' == (object) in 'abc' == o.obj", error);
                } {
                    Eval ("o => 'abc' == o.array", Json, eval, out error);
                    AreEqual("invalid operand: 'abc' == (array) in 'abc' == o.array", error);
                }
                // --- success
                {
                    var result = Filter ("o => 1.1 == 1.1", Json, eval, out _);
                    IsTrue(result);
                }
                // --- null left / right
                {
                    var result = Eval ("o => o.nullVal == 1", Json, eval, out error);
                    IsFalse((bool)result);
                } {
                    var result = Eval ("o => 1 == o.nullVal", Json, eval, out error);
                    IsFalse((bool)result);
                } {
                    var result = Eval ("o => 1.1 == o.nullVal", Json, eval, out error);
                    IsFalse((bool)result);
                } {
                    var result = Eval ("o => 'abc' == o.nullVal", Json, eval, out error);
                    IsFalse((bool)result);
                } {
                    var result = Eval ("o => true == o.nullVal", Json, eval, out error);
                    IsFalse((bool)result);
                }
            }
        }
        
        [Test]
        public static void TestEvalAll() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // --- error
                {
                    Eval ("o => o.array.All(i => i)", Json, eval, out error);
                    AreEqual("quantify operation o.array.All() expect boolean lambda body. Was: i at pos 22", error);
                }
                // --- success
                {
                    var result = Eval ("o => o.array.All(i => i > 0)", Json, eval, out error);
                    IsTrue((bool)result);
                } {
                    var result = Eval ("o => o.array.All(i => i == 1)", Json, eval, out error);
                    IsFalse((bool)result);
                } {
                    var result = Eval ("o => o.unknown.All(i => i == 1)", Json, eval, out error);
                    IsTrue((bool)result);
                }
            }
        }
        
        [Test]
        public static void TestEvalAny() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // --- error
                {
                    Eval ("o => o.array.Any(i => i)", Json, eval, out error);
                    AreEqual("quantify operation o.array.Any() expect boolean lambda body. Was: i at pos 22", error);
                }
                // --- success
                {
                    var result = Eval ("o => o.array.Any(i => i == 1)", Json, eval, out error);
                    IsTrue((bool)result);
                } {
                    var result = Eval ("o => o.array.Any(i => i == 99)", Json, eval, out error);
                    IsFalse((bool)result);
                } {
                    var result = Eval ("o => o.unknown.Any(i => i == 1)", Json, eval, out error);
                    IsFalse((bool)result);
                }
            }
        }
        
        [Test]
        public static void TestEvalCount() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // --- error
                {
                    Eval ("o => o.array.Count(i => i) == 3", Json, eval, out error);
                    AreEqual("quantify operation o.array.Count() expect boolean lambda body. Was: i at pos 24", error);
                }
                // --- success
                {
                    var result = Eval ("o => o.array.Count(i => i == 2) == 1", Json, eval, out error);
                    IsTrue((bool)result);
                } {
                    var result = Eval ("o => o.array.Count(i => i == -99) == 66", Json, eval, out error);
                    IsFalse((bool)result);
                } {
                    var result = Eval ("o => o.unknown.Count() == 0", Json, eval, out error);
                    IsTrue((bool)result);
                } {
                    var result = Eval ("o => o.array.Count() == 3", Json, eval, out error);
                    IsTrue((bool)result);
                }
            }
        }
        
        [Test]
        public static void TestEvalCompare() {
            using (var eval = new JsonEvaluator()) {
                string  error;
                // --- error
                {
                    Eval ("o => o.strVal < 1", Json, eval, out error);
                    AreEqual("incompatible operands: 'abc' < 1 in o.strVal < 1", error);
                } {
                    Eval ("o => o.strVal < o.boolVal", Json, eval, out error);
                    AreEqual("incompatible operands: 'abc' < true in o.strVal < o.boolVal", error);
                } {
                    Eval ("o => o.boolVal < o.strVal", Json, eval, out error);
                    AreEqual("incompatible operands: true < 'abc' in o.boolVal < o.strVal", error);
                } {
                    Eval ("o => 1 < o.strVal", Json, eval, out error);
                    AreEqual("incompatible operands: 1 < 'abc' in 1 < o.strVal", error);
                }
                // --- error: object / array
                {
                    Eval ("o => o.obj < 'abc'", Json, eval, out error);
                    AreEqual("invalid operand: (object) < 'abc' in o.obj < 'abc'", error);
                } {
                    Eval ("o => o.array < 'abc'", Json, eval, out error);
                    AreEqual("invalid operand: (array) < 'abc' in o.array < 'abc'", error);
                } {
                    Eval ("o => 'abc' < o.obj", Json, eval, out error);
                    AreEqual("invalid operand: 'abc' < (object) in 'abc' < o.obj", error);
                } {
                    Eval ("o => 'abc' < o.array", Json, eval, out error);
                    AreEqual("invalid operand: 'abc' < (array) in 'abc' < o.array", error);
                }
                // --- success
                {
                    var result = Filter ("o => 1 < 1.1", Json, eval, out _);
                    IsTrue(result);
                } {
                    var result = Filter ("o => 1.1 > 1", Json, eval, out _);
                    IsTrue(result);
                } {
                    var result = Filter ("o => 1.1 < 1.2", Json, eval, out _);
                    IsTrue(result);
                } {
                    var result = Eval ("1 == 1", Json, eval, out _);
                    AreEqual(true, result);
                }
                // --- null
                {
                    var result = Filter ("o => o.nullVal < 1", Json, eval, out _);
                    IsFalse(result);
                } {
                    var result = Filter ("o => 1 < o.nullVal", Json, eval, out _);
                    IsFalse(result);
                } {
                    var result = Filter ("o => 1.1 < o.nullVal", Json, eval, out _);
                    IsFalse(result);
                } {
                    var result = Filter ("o => 'abc' < o.nullVal", Json, eval, out _);
                    IsFalse(result);
                } {
                    // Abs(null) = null, 1 < null = false (as ISO SQL)
                    var result = Eval ("o => 1 < Abs(o.nullVal)", Json, eval, out error);
                    IsNull(result);
                }
            }
        }
        
        private static object Eval(string operation, string json, JsonEvaluator eval, out string error) {
            var op      = QueryBuilder.Parse(operation, out error);
            if (error != null)
                return null;
            AreEqual(operation, op.Linq);
            var lambda  = new JsonLambda(op);
            var value   = new JsonValue(json);
            var result  = eval.Eval(value, lambda, out error);
            return result;
        }
        
        private static bool Filter(string operation, string json, JsonEvaluator eval, out string error) {
            var op      = (FilterOperation)QueryBuilder.Parse(operation, out error);
            if (error != null)
                return false;
            AreEqual(operation, op.Linq);
            var filter  = new JsonFilter(op);
            var value   = new JsonValue(json);
            var result  = eval.Filter(value, filter, out error);
            return result;
        }
        
        // ------------------------ evaluate operations ------------------------ 
        [Test]
        public static void TestFilterUndefinedScalar() {
            using (var eval = new JsonEvaluator()) {
                // use an aggregate (Max) of an empty array and compare it to a scalar => Max([]) == null
                // Equality compare n== != with null
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) == null", true,  eval);
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) == 1",    false, eval);
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) != 1",    true,  eval);
                // Ordering operators: <, <=, >, >= with null with Long or Double is always false (as ISO SQL)
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) <  1",    false, eval);
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) <= 1",    false, eval);
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) >  1",    false, eval);
                AssertFilterUndefinedScalar ("o => o.items.Max(item => item.amount) >= 1",    false, eval);
            }
        }
        
        private static void AssertFilterUndefinedScalar(string operation, bool expect, JsonEvaluator eval) {
            var json    = @"{ ""items"": [] }";
            var op      = (FilterOperation)QueryBuilder.Parse(operation, out _);
            var filter  = new JsonFilter(op);
            var value   = new JsonValue(json);
            var result  = eval.Filter(value, filter, out _);
            
            AreEqual(expect, result);
        } 
    }
}