// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Query;

namespace Friflo.Json.Flow.Graph
{
    public class JsonEvaluator : IDisposable
    {
        private readonly ScalarSelector   scalarSelector    = new ScalarSelector();

        public void Dispose() {
            scalarSelector.Dispose();
        }

        // --- Filter
        public bool Filter(string json, JsonFilter filter) {
            ReadJsonFields(json, filter);
            var cx = new EvalCx(-1);
            var evalResult = filter.op.Eval(cx);
            
            foreach (var result in evalResult.values) {
                if (result.CompareTo(Operator.True) != 0)
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
        internal            Operator            op;
        private  readonly   OperatorContext     operatorContext = new OperatorContext();

        public   override   string              ToString() => op != null ? op.ToString() : "not initialized";

        internal JsonLambda() { }

        public JsonLambda(Operator op) {
            InitLambda(op);
        }
        
        public static JsonLambda Create<T> (Expression<Func<T, object>> lambda) {
            var op = Operator.FromLambda(lambda);
            var jsonLambda = new JsonLambda(op);
            return jsonLambda;
        }

        private void InitLambda(Operator op) {
            this.op = op;
            operatorContext.Init();
            op.Init(operatorContext);
            selectors.Clear();
            fields.Clear();
            foreach (var selector in operatorContext.selectors) {
                selectors.Add(selector.field);
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
        public JsonFilter(BoolOp op) : base(op) { }
        
        public static JsonFilter Create<T> (Expression<Func<T, bool>> filter) {
            var op = Operator.FromFilter(filter);
            var jsonLambda = new JsonFilter(op);
            return jsonLambda;
        }
    }
}
