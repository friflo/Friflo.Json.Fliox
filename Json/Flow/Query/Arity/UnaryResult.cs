// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Friflo.Json.Flow.Select;

namespace Friflo.Json.Flow.Query.Arity
{
    // ------------------------------------- UnaryResult -------------------------------------
    internal readonly struct UnaryValue {
        internal readonly   Scalar value;

        internal UnaryValue(Scalar value) {
            this.value  = value;
        }
    }
    
    internal struct UnaryResultEnumerator : IEnumerator<UnaryValue>
    {
        private readonly    Scalar?      value;
        private readonly    EvalResult   result;
        
        private readonly    int          last;
        private             int          pos;
        
        internal UnaryResultEnumerator(UnaryResult binaryResult) {
            result  = binaryResult.result;
            if (result.Count == 1)
                value = result.values[0];
            else {
                value = null;
            }
            last = result.Count - 1;
            pos = -1;
        }
        
        public bool MoveNext() {
            if (pos == last)
                return false;
            pos++;
            return true;
        }

        public void Reset() { pos = -1; }

        public UnaryValue Current {
            get {
                var val  = value  ?? result.values [pos];
                return new UnaryValue(val);
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose() { }
    } 
    
    internal readonly struct  UnaryResult : IEnumerable<UnaryValue>
    {
        internal  readonly EvalResult   result;

        internal UnaryResult(EvalResult result) {
            this.result  = result;
            if (result.Count == 1)
                return;
            throw new InvalidOperationException("Expect at least an operation result with one element");
        }

        public IEnumerator<UnaryValue> GetEnumerator() {
            return new UnaryResultEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}