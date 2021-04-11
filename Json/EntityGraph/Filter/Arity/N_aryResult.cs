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
        internal readonly EvalResult values;

        internal N_aryList(int capacity) {
            values = new EvalResult(new List<SelectorValue>(capacity));
        }
    }
    
    internal struct N_aryResultEnumerator : IEnumerator<N_aryList>
    {
        private readonly    EvalResult                  singleValues;
        private readonly    List<EvalResult>            values;
        private readonly    int                         last;
        private             int                         pos;
        
        internal N_aryResultEnumerator(N_aryResult binaryResult) {
            values       = binaryResult.values;
            singleValues = new EvalResult(new List<SelectorValue>(values.Count));
            foreach (var value in values) {
                singleValues.Add(value. Count == 1 ? value.values [0] : null);
            }
            last = values.Max(value => value.Count) - 1;
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
                var resultList = new N_aryList(singleValues.Count);
                for (int n = 0; n < singleValues.Count; n++) {
                    var single = singleValues.values[n];
                    var result  = single ?? values[n].values[pos];
                    resultList.values.Add(result);
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