// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    public class JsonFilter : IDisposable
    {
        private readonly JsonSelector jsonSelector = new JsonSelector();

        public void Dispose() {
            jsonSelector.Dispose();
        }

        public bool Filter(string json, string filter) {
            return true;
        }
        
        public bool Filter(string json, BoolOp filter) {
            var cx = new GraphOpContext();
            filter.Init(cx);
            var selectorResults = jsonSelector.Select(json, cx.selectors.Keys.ToList());
            int index = 0;
            foreach (var selectorPair in cx.selectors) {
                Field field = selectorPair.Value;
                field.values = selectorResults[index++].values;
            }

            var evalResult = filter.Eval();
            if (evalResult.Count == 1 && evalResult[0].CompareTo(Operator.True) == 0)
                return true;
            return false;
        }

    }
}