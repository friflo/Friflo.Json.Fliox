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
                case JsonKeyType.Long:
                    long longDif = x.lng - y.lng;
                    if (longDif < 0)
                        return -1;
                    if (longDif > 0)
                        return +1;
                    return 0;
                case JsonKeyType.String:
                    return string.Compare(x.str, y.str, StringComparison.InvariantCulture);
                case JsonKeyType.Guid:
                    return x.guid.CompareTo(y.guid);
                default:
                    throw new InvalidOperationException("Invalid IdType"); 
            }
        }
    }
    
    public sealed class JsonKeyEqualityComparer : IEqualityComparer<JsonKey>
    {
        public bool Equals(JsonKey x, JsonKey y) {
            if (x.type != y.type)
                return false;
            
            switch (x.type) {
                case JsonKeyType.Long:      return x.lng  == y.lng;
                case JsonKeyType.String:    return x.str  == y.str;
                case JsonKeyType.Guid:      return x.guid == y.guid;
                default:
                    throw new InvalidOperationException("Invalid IdType"); 
            }
        }

        public int GetHashCode(JsonKey jsonKey) {
            switch (jsonKey.type) {
                case JsonKeyType.Long:      return jsonKey.lng. GetHashCode();
                case JsonKeyType.String:    return jsonKey.str. GetHashCode();
                case JsonKeyType.Guid:      return jsonKey.guid.GetHashCode();
                default:
                    throw new InvalidOperationException("Invalid IdType"); 
            }
        }
    }
}