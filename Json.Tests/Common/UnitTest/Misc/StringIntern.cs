// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.LabString
{
    public class StringIntern
    {
        private readonly    Dictionary<StringHash, string>  stringMap       = new Dictionary<StringHash, string>(Equality);
        
        public string Get(string str) {
            var strHash = new StringHash(str);
            lock (stringMap) {
                if (stringMap.TryGetValue(strHash, out string value))
                    return value;
                return stringMap[strHash] = str;
            }
        }
        
        private readonly struct StringHash
        {
            internal readonly string value;
            internal readonly int    hashCode;
            
            internal StringHash(string value) {
                this.value  = value;
                hashCode    = value.GetHashCode();
            }
            
            internal bool IsEqual(in StringHash other) {
                return hashCode == other.hashCode && value == other.value;
            }
        }
        
        private static readonly  StringComparer Equality = new StringComparer();

        private sealed class StringComparer : IEqualityComparer<StringHash>
        {
            public bool Equals(StringHash x, StringHash y) {
                return x.hashCode == y.hashCode && x.value == y.value;
            }

            public int GetHashCode(StringHash value) {
                return value.hashCode;
            }
        }
    }
}