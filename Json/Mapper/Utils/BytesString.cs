// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Burst;

namespace Friflo.Json.Mapper.Utils
{
    public class BytesString
    {
        public Bytes value;
        
        public BytesString() {
        }

        public BytesString(string str) {
            value = new Bytes(str);
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            if (obj is BytesString other)
                return value.IsEqualBytes(ref other.value);
            return false;
        }

        public override int GetHashCode() {
            return value.GetHashCode();
        }

        public override string ToString() {
            return value.ToString();
        }
    }
}