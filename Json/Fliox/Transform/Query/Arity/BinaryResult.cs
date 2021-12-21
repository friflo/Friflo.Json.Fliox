// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Friflo.Json.Fliox.Transform.Query.Arity
{
    // ------------------------------------- BinaryResult -------------------------------------
    internal readonly struct BinaryPair {
        internal readonly   Scalar left;
        internal readonly   Scalar right;

        internal BinaryPair(in Scalar left, in Scalar right) {
            this.left  = left;
            this.right = right;
        }

        public override string ToString() => $"({left}, {right})";
    }
    
    internal struct BinaryResultEnumerator : IEnumerator<BinaryPair>
    {
        private readonly    Scalar?      leftValue;
        private readonly    Scalar?      rightValue;
        private readonly    EvalResult   leftResult;
        private readonly    EvalResult   rightResult;
        private readonly    int          last;
        private             int          pos;
        
        internal BinaryResultEnumerator(BinaryResult binaryResult) {
            leftResult  = binaryResult.leftResult;
            rightResult = binaryResult.rightResult;
            if (leftResult.Count == 1)
                leftValue = leftResult.values[0];
            else {
                leftValue = null;
            }
            if (rightResult.Count == 1)
                rightValue = rightResult.values[0];
            else
                rightValue = null;
            last = GetLast(leftResult, rightResult);
            pos = -1;
        }

        private static int GetLast(EvalResult leftResult, EvalResult rightResult) {
            if (leftResult.Count == 0 || rightResult.Count == 0)
                return -1; // if only one of the results is empty the iterator return no elements
            return Math.Max(leftResult.Count, rightResult.Count) - 1;
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
                var left  = leftValue  ?? leftResult.values [leftResult.StartIndex  + pos];
                var right = rightValue ?? rightResult.values[rightResult.StartIndex + pos];
                return new BinaryPair(left, right);
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose() { }
    } 
    
    internal readonly struct  BinaryResult // : IEnumerable <BinaryPair>   <- not implemented to avoid boxing
    {
        internal  readonly EvalResult   leftResult;
        internal  readonly EvalResult   rightResult;

        internal BinaryResult(EvalResult leftResult, EvalResult rightResult) {
            this.leftResult  = leftResult;
            this.rightResult = rightResult;
            if (leftResult.Count == 1 || rightResult.Count == 1)
                return;
            throw new InvalidOperationException("Expect at least an operation result with one element");
        }

        // return BinaryResultEnumerator instead of IEnumerator<BinaryPair> to avoid boxing 
        public BinaryResultEnumerator GetEnumerator() {
            return new BinaryResultEnumerator(this);
        }
        
        // IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }  see boxing note above
    }
}