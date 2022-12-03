using System;
using System.Collections.Generic;

namespace Friflo.Json.Burst
{
    public readonly struct BytesHash : IDisposable
    {
        public readonly int     hc;
        public readonly Bytes   value;

        public static readonly  BytesHashComparer Equality = new BytesHashComparer();

        
        public BytesHash(in Bytes value) {
            var array   = value.buffer.array;
            int h       = value.Len;
            // Rotate by 3 bits and XOR the new value.
            for (int i = value.start; i < value.end; i++) {
                h = (h << 3) | (h >> (29)) ^ array[i];
            }
            hc          = Math.Abs(h);
            this.value  = value;
        }

        public void Dispose() {
            value.Dispose();
        }
    }
    
    public sealed class BytesHashComparer : IEqualityComparer<BytesHash>
    {
        public bool Equals(BytesHash x, BytesHash y) {
            return x.value.IsEqualBytes(y.value);
        }

        public int GetHashCode(BytesHash value) {
            return value.hc;
        }
    }
}