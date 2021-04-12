// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    public class JsonEvaluator : IDisposable
    {
        private readonly JsonSelector       jsonSelector    = new JsonSelector();
        private readonly List<string>       selectors       = new List<string>();
        private readonly List<Field>        fields          = new List<Field>();
        private readonly OperatorContext    operatorContext = new OperatorContext();

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
            ReadOperatorFields(json, filter);
            
            var evalResult = filter.Eval();
            
            foreach (var result in evalResult.values) {
                if (result.CompareTo(Operator.True) != 0)
                    return false;
            }
            return true;
        }
        
        public object Eval(string json, Operator op) {
            ReadOperatorFields(json, op);

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

        private void ReadOperatorFields(string json, Operator op) {
            operatorContext.Init();
            op.Init(operatorContext);
            selectors.Clear();
            fields.Clear();
            foreach (var selectorPair in operatorContext.selectors) {
                selectors.Add(selectorPair.Key);
                fields.Add(selectorPair.Value);
            }
            var selectorResults = jsonSelector.Select(json, selectors);
            for (int n = 0; n < fields.Count; n++) {
                Field field = fields[n];
                field.evalResult = new EvalResult(selectorResults[n].values);
            }
        }

    }
}