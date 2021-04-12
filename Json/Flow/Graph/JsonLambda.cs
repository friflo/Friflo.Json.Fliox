// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Query;

namespace Friflo.Json.Flow.Graph
{
    public class JsonLambda
    {
        private  readonly   List<string>        selectors       = new List<string>();
        internal readonly   List<Field>         fields          = new List<Field>();
        internal readonly   JsonSelect          jsonSelect      = new JsonSelect();
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

        internal void InitLambda(Operator op) {
            this.op = op;
            operatorContext.Init();
            op.Init(operatorContext);
            selectors.Clear();
            fields.Clear();
            foreach (var selectorPair in operatorContext.selectors) {
                selectors.Add(selectorPair.Key);
                fields.Add(selectorPair.Value);
            }
            jsonSelect.CreateNodeTree(selectors);
        }
    }

    public class JsonFilter : JsonLambda
    {
        public JsonFilter(BoolOp op) : base(op) { }
        
        public static JsonFilter Create<T> (Expression<Func<T, bool>> filter) {
            var op = (BoolOp)Operator.FromFilter(filter);
            var jsonLambda = new JsonFilter(op);
            return jsonLambda;
        }
    }
}
