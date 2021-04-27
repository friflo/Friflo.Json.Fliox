// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Transform.Query;
using Friflo.Json.Flow.Transform.Query.Ops;

namespace Friflo.Json.Flow.Transform
{
    public class JsonEvaluator : IDisposable
    {
        private readonly ScalarSelector   scalarSelector    = new ScalarSelector();

        public void Dispose() {
            scalarSelector.Dispose();
        }

        // --- Filter
        public bool Filter(string json, JsonFilter filter) {
            if (filter.op is TrueLiteral)
                return true;  // result is independent fom given json
            if (filter.op is FalseLiteral)
                return false; // result is independent fom given json
            
            ReadJsonFields(json, filter);
            var cx = new EvalCx(-1);
            var evalResult = filter.op.Eval(cx);

            if (evalResult.values.Count == 0)
                return false;
            
            foreach (var result in evalResult.values) {
                if (result.CompareTo(Operation.True) != 0)
                    return false;
            }
            return true;
        }

        // --- Eval
        public object Eval(string json, JsonLambda lambda) {
            ReadJsonFields(json, lambda);
            var cx = new EvalCx(-1);
            var evalResult = lambda.op.Eval(cx);
            
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
            var query = lambda.scalarSelect;
            scalarSelector.Select(json, query);
            var fields = lambda.fields;
            for (int n = 0; n < fields.Count; n++) {
                Field field = fields[n];
                var evalResult = lambda.resultBuffer[n];
                evalResult.SetRange(0, evalResult.values.Count);
                field.evalResult = evalResult;
            }
        }
    }
    
    // --------------------------------------- JsonLambda ---------------------------------------
    public class JsonLambda
    {
        private  readonly   List<string>        selectors       = new List<string>();        
        internal readonly   List<Field>         fields          = new List<Field>();        // Count == selectors.Count
        internal readonly   List<EvalResult>    resultBuffer    = new List<EvalResult>();   // Count == selectors.Count
        internal readonly   ScalarSelect        scalarSelect    = new ScalarSelect();
        internal            Operation           op;
        private  readonly   OperationContext    operationContext = new OperationContext();

        public   override   string              ToString() => op != null ? op.Linq : "not initialized";

        internal JsonLambda() { }

        public JsonLambda(Operation op) {
            InitLambda(op);
        }
        
        public static JsonLambda Create<T> (Expression<Func<T, object>> lambda) {
            var op = Operation.FromLambda(lambda);
            var jsonLambda = new JsonLambda(op);
            return jsonLambda;
        }

        private void InitLambda(Operation op) {
            this.op = op;
            operationContext.Init();
            op.Init(operationContext, 0);
            selectors.Clear();
            fields.Clear();
            foreach (var selector in operationContext.selectors) {
                selectors.Add(selector.selector);
                fields.Add(selector);
            }
            scalarSelect.CreateNodeTree(selectors);

            var pathSelectors = scalarSelect.nodeTree.selectors;
            resultBuffer.Clear();
            for (int n = 0; n < pathSelectors.Count; n++) {
                var result = pathSelectors[n].result;
                var evalResult = new EvalResult(result.values, result.groupIndices);
                resultBuffer.Add(evalResult);
            }
        }
    }

    public class JsonFilter : JsonLambda
    {
        public JsonFilter(FilterOperation op) : base(op) { }
        
        public static JsonFilter Create<T> (Expression<Func<T, bool>> filter) {
            var op = Operation.FromFilter(filter);
            var jsonLambda = new JsonFilter(op);
            return jsonLambda;
        }
    }
}
