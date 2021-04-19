// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Flow.Graph.Query.Arity
{
    // ------------------------------------- BinaryResult -------------------------------------
    internal readonly struct N_aryList {
        internal  readonly  EvalResult  evalResult;
        
        public    override  string      ToString() => $"({string.Join(", ", evalResult.values)})";

        internal N_aryList(int capacity) {
            evalResult = new EvalResult(new List<Scalar>(capacity));
        }
    }
    
    internal struct N_aryResultEnumerator : IEnumerator<N_aryList>
    {
        private readonly    List<Scalar?>       values;
        private readonly    N_aryList           resultList;
        private             List<EvalResult>    evalResults;
        private             int                 last;
        private             int                 pos;
        
        internal N_aryResultEnumerator(N_aryResult binaryResult) {
            evalResults = binaryResult.results;
            values      = new List<Scalar?>(evalResults.Count);
            resultList  = new N_aryList(evalResults.Count);
            last        = GetLast(evalResults);
            pos         = -1;
            SetValues();
        }
        
        internal void Init(N_aryResult binaryResult) {
            evalResults = binaryResult.results;
            values.Clear();
            last        = GetLast(evalResults);
            pos         = -1;
            SetValues();
        }
        
        internal N_aryResultEnumerator(bool _) {
            evalResults = null;
            values      = new List<Scalar?>(0);
            resultList  = new N_aryList(0);
            last        = -1;
            pos         = -1;
        }
        
        private void SetValues() {
            foreach (var result in evalResults) {
                if (result.Count == 1)
                    values.Add(result.values[0]);
                else
                    values.Add(null);
            }
        }

        private static int GetLast(List<EvalResult> results) {
            int max = results[0].Count;
            for (int n = 1; n < results.Count; n++) {
                var count = results[n].Count;
                if (count == 0)
                    return -1; // if only one of the results is empty the iterator return no elements
                if (max < count)
                    max = count;
            }
            return max - 1;
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
                resultList.evalResult.Clear();
                for (int n = 0; n < values.Count; n++) {
                    var single = values[n];
                    var result  = single ?? evalResults[n].values[evalResults[n].StartIndex + pos];
                    resultList.evalResult.Add(result);
                }
                return resultList;
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose() { }
    } 
    
    internal readonly struct  N_aryResult // : IEnumerable <BinaryPair>   <- not implemented to avoid boxing
    {
        internal  readonly  List<EvalResult>    results;
        
        internal N_aryResult(List<EvalResult> results) {
            this.results  = results;
        }

        // return N_aryResultEnumerator instead of IEnumerator<BinaryPair> to avoid boxing 
        public N_aryResultEnumerator GetEnumerator() {
            return new N_aryResultEnumerator(this);
        }

        // IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }  see boxing note above
    }
}