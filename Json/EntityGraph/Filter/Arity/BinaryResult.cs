// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter.Arity
{
    // ------------------------------------- BinaryResult -------------------------------------
    internal readonly struct BinaryPair {
        internal readonly SelectorValue left;
        internal readonly SelectorValue right;

        internal BinaryPair(SelectorValue left, SelectorValue right) {
            this.left  = left;
            this.right = right;
        }
    }
    
    internal struct BinaryResultEnumerator : IEnumerator<BinaryPair>
    {
        private readonly    SelectorValue       leftValue;
        private readonly    SelectorValue       rightValue;
        private readonly    EvalResult          leftResult;
        private readonly    EvalResult          rightResult;
        private readonly    int                 last;
        private             int                 pos;
        
        internal BinaryResultEnumerator(BinaryResult binaryResult) {
            leftResult  = binaryResult.left;
            rightResult = binaryResult.right;
            leftValue  = leftResult. Count == 1 ? leftResult.values [0] : null;
            rightValue = rightResult.Count == 1 ? rightResult.values[0] : null;
            last = Math.Max(leftResult.Count, rightResult.Count) - 1;
            pos = -1;
        }
        
        public bool MoveNext() {
            if (pos == last)
                return false;
            pos++;
            return true;
        }

        public void Reset() { pos = -1; }

        public BinaryPair Current {
            get {
                var left  = leftValue  ?? leftResult.values [pos];
                var right = rightValue ?? rightResult.values[pos];
                return new BinaryPair(left, right);
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose() { }
    } 
    
    internal readonly struct  BinaryResult : IEnumerable<BinaryPair>
    {
        internal  readonly EvalResult   left;
        internal  readonly EvalResult   right;

        internal BinaryResult(EvalResult left, EvalResult right) {
            this.left  = left;
            this.right = right;
            if (left.Count == 1 || right.Count == 1)
                return;
            throw new InvalidOperationException("Expect at least an operation result with one element");
        }

        public IEnumerator<BinaryPair> GetEnumerator() {
            return new BinaryResultEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}