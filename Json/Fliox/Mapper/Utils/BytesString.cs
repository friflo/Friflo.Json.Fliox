// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Mapper.Utils
{
    internal sealed class BytesString
    {
        internal Bytes value;
        
        internal BytesString() {
        }
        
        internal BytesString(ref Bytes str) {
            value = new Bytes(ref str);
        }

        internal BytesString(string str) {
            value = new Bytes(str, Untracked.Bytes);
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
            return value.AsString();
        }
    }
}