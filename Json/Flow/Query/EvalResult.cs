// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Select;

namespace Friflo.Json.Flow.Query
{
    internal readonly struct EvalResult
    {
        internal readonly  List<Scalar> values;

        internal EvalResult (Scalar singleValue) {
            values = new List<Scalar> { singleValue };
        }
        
        internal EvalResult (List<Scalar> values) {
            this.values = values;
        }

        internal int Count => values.Count;

        internal void Clear() {
            values.Clear();
        }
        
        internal void Add(Scalar value) {
            values.Add(value);
        }
        
        internal void SetSingle(Scalar value) {
            values[0] = value;
        }
        
    }
}