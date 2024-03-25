// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.Mapper
{
    internal sealed class ShortStringEqualityComparer : IEqualityComparer<ShortString>
    {
        public bool Equals(ShortString x, ShortString y) {
            return x.IsEqual(y);
        }

        public int GetHashCode(ShortString jsonKey) {
            return jsonKey.HashCode();
        }
    }
    
    internal sealed class ShortStringComparer : IComparer<ShortString>
    {
        public int Compare(ShortString x, ShortString y) {
            return x.Compare(y);
        }
    }
}