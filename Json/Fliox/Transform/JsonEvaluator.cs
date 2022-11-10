// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Transform.Query;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Transform
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class JsonEvaluator : IDisposable
    {
        private readonly ScalarSelector   scalarSelector    = new ScalarSelector();

        public void Dispose() {
            scalarSelector.Dispose();
        }

        // --- Filter
        public bool Filter(in JsonValue json, JsonFilter filter, out string error) {
            if (filter.op is TrueLiteral) {
                error = null;
                return true;  // result is independent fom given json
            }
            if (filter.op is FalseLiteral) {
                error = null;
                return false; // result is independent fom given json
            }
            ReadJsonFields(json, filter);
            var cx = new EvalCx(-1);
            var evalResult = filter.op.Eval(cx);

            if (evalResult.values.Count == 0) {
                error = null;
                return false;
            }
            foreach (var result in evalResult.values) {
                if (!result.IsError)
                    continue;
                error = result.ErrorMessage;
                return false;
            }
            error = null;
            foreach (var result in evalResult.values) {
                var isTrue = result.EqualsTo(Operation.True, null);  
                if (!isTrue.IsTrue)
                    return false;
            }
            return true;
        }

        // --- Eval
        public object Eval(in JsonValue json, JsonLambda lambda, out string error) {
            ReadJsonFields(json, lambda);
            var cx = new EvalCx(-1);
            var evalResult = lambda.op.Eval(cx);
            
            if (evalResult.values.Count == 1) {
                var value = evalResult.values[0];    
                if (value.IsError) {
                    error = value.ErrorMessage;
                    return null;
                }
                error = null;
                return value.AsObject();
            }
            
            object[] evalResults = new object[evalResult.values.Count];
            for (int n = 0; n < evalResult.values.Count; n++) {
                var result = evalResult.values[n];
                if (result.IsError) {
                    error = result.ErrorMessage;
                    return null;
                }
                evalResults[n] = result.AsObject();
            }
            error = null;
            return evalResults;
        }

        private void ReadJsonFields(in JsonValue json, JsonLambda lambda) {
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
        private  readonly   OperationContext    operationContext;

        public              string              Linq        => op != null ? op.Linq : "not initialized";
        public   override   string              ToString()  => Linq;

        internal JsonLambda() { }
        
        public JsonLambda(Operation op) {
            operationContext = new OperationContext();
            operationContext.Init(op, out string error);
            if (error != null)
                throw new InvalidOperationException(error);
            InitLambda(operationContext);
        }
        
        public JsonLambda(OperationContext operationContext) {
            this.operationContext = operationContext;
            InitLambda(operationContext);
        }
        
        public static JsonLambda Create<T> (Expression<Func<T, object>> lambda) {
            var op = Operation.FromLambda(lambda);
            var jsonLambda = new JsonLambda(op);
            return jsonLambda;
        }

        private void InitLambda(OperationContext opCx) {
            op = opCx.Operation;
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

    public sealed class JsonFilter : JsonLambda
    {
        public JsonFilter(FilterOperation  op) : base(op) { }
        public JsonFilter(OperationContext cx) : base(cx) { }
        
        public static JsonFilter Create<T> (Expression<Func<T, bool>> filter) {
            var op = Operation.FromFilter(filter);
            var jsonLambda = new JsonFilter(op);
            return jsonLambda;
        }
        
        public QueryFormat Query { get {
            var filter = (FilterOperation)op;
            return filter.query;
        } }
    }
}
