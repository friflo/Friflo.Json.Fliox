using System;
using System.Collections.Generic;

namespace Friflo.Json.Burst
{
    public readonly struct BytesHash
    {
        public readonly int     hashCode;
        public readonly Bytes   value;

        public static readonly  BytesHashComparer Equality = new BytesHashComparer();

        
        public BytesHash(in Bytes value) {
            var array   = value.buffer.array;
            int len     = value.end - value.start;
            int h       = len;
            // Rotate by 3 bits and XOR the new value.
            for (int i = value.start; i < value.end; i++) {
                h = (h << 3) | (h >> (29)) ^ array[i];
            }
            hashCode            = Math.Abs(h);

            this.value.buffer   = value.buffer;
            this.value.start    = value.start;
            this.value.end      = value.end;
        }
    }
    
    public sealed class BytesHashComparer : IEqualityComparer<BytesHash>
    {
        public bool Equals(BytesHash x, BytesHash y) {
            return x.value.IsEqual(y.value);
        }

        public int GetHashCode(BytesHash value) {
            return value.hashCode;
        }
    }
}