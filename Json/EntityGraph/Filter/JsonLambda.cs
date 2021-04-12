// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    public class JsonLambda
    {
        private  readonly   List<string>        selectors       = new List<string>();
        internal readonly   List<Field>         fields          = new List<Field>();
        internal readonly   JsonSelectorQuery   selectorQuery   = new JsonSelectorQuery();
        internal            Operator            op;
        private  readonly   OperatorContext     operatorContext = new OperatorContext();
        
        
        internal JsonLambda() {}

        public JsonLambda(Operator op) {
            InitLambda(op);
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
            selectorQuery.CreateNodeTree(selectors);
        }
    }
}
