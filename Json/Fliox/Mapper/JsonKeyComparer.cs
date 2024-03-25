// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.Mapper
{
    internal sealed class JsonKeyComparer : IComparer<JsonKey>
    {
        public int Compare(JsonKey x, JsonKey y) {
            return x.Compare(y);
        }
    }
    
    internal sealed class JsonKeyEqualityComparer : IEqualityComparer<JsonKey>
    {
        public bool Equals(JsonKey x, JsonKey y) {
            return x.IsEqual(y);
        }

        public int GetHashCode(JsonKey jsonKey) {
            return jsonKey.HashCode();
        }
    }
}