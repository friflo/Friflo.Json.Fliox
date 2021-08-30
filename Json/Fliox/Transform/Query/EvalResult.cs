// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Friflo.Json.Fliox.Transform.Query
{
    internal class EvalResult
    {
        internal readonly   List<Scalar>    values;
        internal readonly   List<int>       groupIndices;
        private             int             startIndex;
        private             int             endIndex;

        internal            int             StartIndex => startIndex;

        internal EvalResult (Scalar singleValue) {
            values              = new List<Scalar> { singleValue };
            this.groupIndices   = null;
            startIndex          = 0;
            endIndex            = 1;
        }
        
        internal EvalResult (List<Scalar> values) {
            this.values         = values;
            this.groupIndices   = null;
            startIndex          = 0;
            endIndex            = values.Count;
        }
        
        internal EvalResult (List<Scalar> values, List<int> groupIndices) {
            this.values         = values;
            this.groupIndices   = groupIndices;
            startIndex          = 0;
            endIndex            = values.Count;
        }

        internal void SetRange(int startIndex, int endIndex) {
            this.startIndex = startIndex;
            this.endIndex   = endIndex;
        }

        internal int Count => endIndex - startIndex;

        internal void Clear() {
            values.Clear();
            endIndex = 0;
        }
        
        internal void Add(Scalar value) {
            values.Add(value);
            endIndex++;
        }
        
        internal void SetSingle(Scalar value) {
            values[0] = value;
        }
        
    }
}