// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    public class JsonEvaluator : IDisposable
    {
        private readonly JsonSelector jsonSelector = new JsonSelector();

        public void Dispose() {
            jsonSelector.Dispose();
        }

        public bool Filter(string json, string filter) {
            return true;
        }

        public bool Filter<T>(string json, Expression<Func<T, bool>> filter) {
            var op = Operator.FromFilter(filter);
            return Filter(json, (BoolOp)op);
        }

        public bool Filter(string json, BoolOp filter) {
            var cx = new GraphOpContext();
            filter.Init(cx);
            var selectorResults = jsonSelector.Select(json, cx.selectors.Keys.ToList());
            int index = 0;
            foreach (var selectorPair in cx.selectors) {
                Field field = selectorPair.Value;
                field.results = new EvalResult(selectorResults[index++].values);
            }

            var evalResult = filter.Eval();
            
            foreach (var result in evalResult.values) {
                if (result.CompareTo(Operator.True) != 0)
                    return false;
            }
            return true;
        }
        
        public object Eval(string json, Operator op) {
            var cx = new GraphOpContext();
            op.Init(cx);
            var selectorResults = jsonSelector.Select(json, cx.selectors.Keys.ToList());
            int index = 0;
            foreach (var selectorPair in cx.selectors) {
                Field field = selectorPair.Value;
                field.results = new EvalResult(selectorResults[index++].values);
            }

            var evalResult = op.Eval();
            if (evalResult.values.Count == 1)
                return evalResult.values[0].AsObject();
            
            object[] evalResults = new object[evalResult.values.Count];
            for (int n = 0; n < evalResult.values.Count; n++) {
                var result = evalResult.values[n];
                evalResults[n] = result.AsObject();
            }
            return evalResults;
        }

    }
}