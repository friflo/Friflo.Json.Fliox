// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Mapper.Graph;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.EntityGraph.Filter.Arity
{
    // ------------------------------------- BinaryResult -------------------------------------
    internal readonly struct N_aryList {
        internal readonly EvalResult evalResult;

        internal N_aryList(int capacity) {
            evalResult = new EvalResult(new List<SelectorValue>(capacity));
        }
    }
    
    internal struct N_aryResultEnumerator : IEnumerator<N_aryList>
    {
        private readonly    EvalResult                  evalResult;
        private readonly    List<EvalResult>            evalResults;
        private readonly    int                         last;
        private             int                         pos;
        
        internal N_aryResultEnumerator(N_aryResult binaryResult) {
            evalResults       = binaryResult.values;
            evalResult = new EvalResult(new List<SelectorValue>(evalResults.Count));
            foreach (var result in evalResults) {
                evalResult.Add(result. Count == 1 ? result.values [0] : null);
            }
            last = evalResults.Max(value => value.Count) - 1;
            pos = -1;
        }
        
        public bool MoveNext() {
            if (pos == last)
                return false;
            pos++;
            return true;
        }

        public void Reset() { pos = -1; }

        public N_aryList Current {
            get {
                var resultList = new N_aryList(evalResult.Count);
                for (int n = 0; n < evalResult.Count; n++) {
                    var single = evalResult.values[n];
                    var result  = single ?? evalResults[n].values[pos];
                    resultList.evalResult.Add(result);
                }
                return resultList;
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose() { }
    } 
    
    internal readonly struct  N_aryResult : IEnumerable<N_aryList>
    {
        internal  readonly List<EvalResult>   values;

        internal N_aryResult(List<EvalResult> values) {
            this.values  = values;
        }

        public IEnumerator<N_aryList> GetEnumerator() {
            return new N_aryResultEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}