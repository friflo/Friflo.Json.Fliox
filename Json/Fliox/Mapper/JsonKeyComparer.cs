// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Fliox.Mapper
{
    public sealed class JsonKeyComparer : IComparer<JsonKey>
    {
        public int Compare(JsonKey x, JsonKey y) {
            int dif = x.type - y.type;
            if (dif != 0)
                return dif;
            
            switch (x.type) {
                case JsonKeyType.LONG:
                    long longDif = x.lng - y.lng;
                    if (longDif < 0)
                        return -1;
                    if (longDif > 0)
                        return +1;
                    return 0;
                case JsonKeyType.STRING:
                    return JsonKey.StringCompare(x, y, StringComparison.InvariantCulture);
                case JsonKeyType.GUID:
                    return x.Guid.CompareTo(y.Guid);
                default:
                    throw new InvalidOperationException("Invalid IdType"); 
            }
        }
    }
    
    public sealed class JsonKeyEqualityComparer : IEqualityComparer<JsonKey>
    {
        public bool Equals(JsonKey x, JsonKey y) {
            return x.IsEqual(y);
        }

        public int GetHashCode(JsonKey jsonKey) {
            return jsonKey.HashCode();
        }
    }
}