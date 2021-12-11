// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public static class TransformExtensions
    {
        public static object Eval (this JsonEvaluator evaluator, string json, JsonLambda lambda) {
            return evaluator.Eval(new JsonValue(json), lambda, out _);
        }
        
        public static bool Filter(this JsonEvaluator evaluator, string json, JsonFilter filter) {
            return evaluator.Filter(new JsonValue(json), filter, out _);
        }
        
        public static IReadOnlyList<ScalarSelectResult> Select(this ScalarSelector selector, string json, ScalarSelect scalarSelect) {
            return selector.Select(new JsonValue(json), scalarSelect);
        }
    }
}