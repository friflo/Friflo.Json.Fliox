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
        internal readonly EvalResult result;

        internal N_aryList(int capacity) {
            result = new EvalResult(new List<SelectorValue>(capacity));
        }
    }
    
    internal struct N_aryResultEnumerator : IEnumerator<N_aryList>
    {
        private readonly    EvalResult                  singleResult;
        private readonly    List<EvalResult>            results;
        private readonly    int                         last;
        private             int                         pos;
        
        internal N_aryResultEnumerator(N_aryResult binaryResult) {
            results       = binaryResult.values;
            singleResult = new EvalResult(new List<SelectorValue>(results.Count));
            foreach (var result in results) {
                singleResult.Add(result. Count == 1 ? result.values [0] : null);
            }
            last = results.Max(value => value.Count) - 1;
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
                var resultList = new N_aryList(singleResult.Count);
                for (int n = 0; n < singleResult.Count; n++) {
                    var single = singleResult.values[n];
                    var result  = single ?? results[n].values[pos];
                    resultList.result.Add(result);
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