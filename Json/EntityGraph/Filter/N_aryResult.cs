// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using Friflo.Json.Mapper.Graph;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.EntityGraph.Filter
{
    // ------------------------------------- BinaryResult -------------------------------------
    internal readonly struct N_aryPair {
        internal readonly SelectorValue left;
        internal readonly SelectorValue right;

        internal N_aryPair(SelectorValue left, SelectorValue right) {
            this.left  = left;
            this.right = right;
        }
    }
    
    internal struct N_aryResultEnumerator : IEnumerator<N_aryPair>
    {
        private readonly    SelectorValue       singleLeft;
        private readonly    SelectorValue       singleRight;
        private readonly    List<SelectorValue> left;
        private readonly    List<SelectorValue> right;
        private readonly    int                 last;
        private             int                 pos;
        
        internal N_aryResultEnumerator(N_aryResult binaryResult) {
            left  = binaryResult.left;
            right = binaryResult.right;
            singleLeft  = left. Count == 1 ? left [0] : null;
            singleRight = right.Count == 1 ? right[0] : null;
            last = Math.Max(left.Count, right.Count) - 1;
            pos = -1;
        }
        
        public bool MoveNext() {
            if (pos == last)
                return false;
            pos++;
            return true;
        }

        public void Reset() { pos = -1; }

        public N_aryPair Current {
            get {
                var leftResult  = singleLeft  ?? left [pos];
                var rightResult = singleRight ?? right[pos];
                return new N_aryPair(leftResult, rightResult);
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose() { }
    } 
    
    internal readonly struct  N_aryResult : IEnumerable<N_aryPair>
    {
        internal  readonly List<SelectorValue>   left;
        internal  readonly List<SelectorValue>   right;

        internal N_aryResult(List<SelectorValue> left, List<SelectorValue> right) {
            this.left  = left;
            this.right = right;
            if (left.Count == 1 || right.Count == 1)
                return;
            throw new InvalidOperationException("Expect at least an operation result with one element");
        }

        public IEnumerator<N_aryPair> GetEnumerator() {
            return new N_aryResultEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}