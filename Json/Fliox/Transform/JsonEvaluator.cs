// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Transform.Query;
using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Transform.Tree;

namespace Friflo.Json.Fliox.Transform
{
    [CLSCompliant(true)]
    public sealed class JsonEvaluator : IDisposable
    {
        private readonly    JsonAstReader   astReader       = new JsonAstReader();

        public void Dispose() {
            astReader.Dispose();
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
            AddLambdaValue(json, filter);
            var cx      = new EvalCx(filter.operationContext);
            var value   = filter.op.Eval(cx);
            if (value.IsError) {
                error = value.ErrorMessage;
                return false;
            }
            error = null;
            var isTrue = value.EqualsTo(Operation.True, null);  
            return isTrue.IsTrue;
        }

        // --- Eval
        public object Eval(in JsonValue json, JsonLambda lambda, out string error) {
            AddLambdaValue(json, lambda);
            var cx      = new EvalCx(lambda.operationContext);
            var value   = lambda.op.Eval(cx);
            if (value.IsError) {
                error = value.ErrorMessage;
                return null;
            }
            error = null;
            return value.AsObject();
        }
        
        private void AddLambdaValue(in JsonValue json, JsonLambda lambda) {
            lambda.operationContext.Reset();
            var arg = lambda.op.GetArg();
            if (arg != null) {
                if (arg.Contains("'")) throw new InvalidOperationException("lambda arg must not contain '.'");
                var ast = astReader.CreateAst(json); // AST_PATH
                lambda.operationContext.AddArgValue(new ArgValue(arg, ast, 0));
            }
        }
    }
    
    // --------------------------------------- JsonLambda ---------------------------------------
    public class JsonLambda
    {
        internal            Operation           op;
        internal readonly   OperationContext    operationContext;

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
    }
}
