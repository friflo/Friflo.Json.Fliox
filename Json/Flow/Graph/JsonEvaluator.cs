// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph.Query;

namespace Friflo.Json.Flow.Graph
{
    public class JsonEvaluator : IDisposable
    {
        private readonly JsonSelector   jsonSelector    = new JsonSelector();

        public void Dispose() {
            jsonSelector.Dispose();
        }

        // --- Filter
        public bool Filter(string json, JsonFilter filter) {
            ReadJsonFields(json, filter);
            
            var evalResult = filter.op.Eval();
            
            foreach (var result in evalResult.values) {
                if (result.CompareTo(Operator.True) != 0)
                    return false;
            }
            return true;
        }

        // --- Eval
        public object Eval(string json, JsonLambda lambda) {
            ReadJsonFields(json, lambda);

            var evalResult = lambda.op.Eval();
            
            if (evalResult.values.Count == 1)
                return evalResult.values[0].AsObject();
            
            object[] evalResults = new object[evalResult.values.Count];
            for (int n = 0; n < evalResult.values.Count; n++) {
                var result = evalResult.values[n];
                evalResults[n] = result.AsObject();
            }
            return evalResults;
        }

        private void ReadJsonFields(string json, JsonLambda lambda) {
            var query = lambda.jsonSelect;
            jsonSelector.Select(json, query);
            var selectorResults = query.GetResult();
            var fields = lambda.fields;
            for (int n = 0; n < fields.Count; n++) {
                Field field = fields[n];
                field.evalResult = new EvalResult(selectorResults[n].values);
            }
        }

    }
}