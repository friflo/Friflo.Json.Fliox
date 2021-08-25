// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Flow.Mapper
{
    public readonly struct JsonKey
    {
        internal readonly   KeyType     type;
        internal readonly   string      str;
        internal readonly   long        lng;
        
        public override string ToString() => AsString();
        
        public static readonly KeyComparer          Comparer = new KeyComparer();
        public static readonly KeyEqualityComparer  Equality = new KeyEqualityComparer();

        public JsonKey (string str) {
            if (long.TryParse(str, out long result)) {
                this.type   = KeyType.Long;
                this.str    = str;
                this.lng    = result;
                return;                   
            }
            this.type   = KeyType.String;
            this.str    = str;
            this.lng    = 0;
        }
        
        public JsonKey (long lng) {
            this.type   = KeyType.Long;
            this.str    = null;
            this.lng    = lng;
        }
        
        public bool IsNull() {
            switch (type) {
                case KeyType.String:    return str == null;
                case KeyType.Long:      return false;
                case KeyType.None:      return true;
                default:
                    throw new InvalidOperationException($"invalid JsonKey: {ToString()}");
            }
        }
        public bool IsEqual(in JsonKey other) {
            if (type != other.type)
                return false;
            
            switch (type) {
                case KeyType.String:    return str == other.str;
                case KeyType.Long:      return lng == other.lng;
                case KeyType.None:      return true;
                default:
                    throw new InvalidOperationException("Invalid IdType"); 
            }
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException("not implemented by intention. Use JsonKey.Equality");
        }

        public override int GetHashCode() {
            switch (type) {
                case KeyType.String:    return str.GetHashCode();
                case KeyType.Long:      return lng.GetHashCode();
                case KeyType.None:      return 0;
                default:
                    throw new InvalidOperationException("cannot be reached");
            }
        }

        public string AsString() {
            switch (type) {
                case KeyType.String:    return str;
                case KeyType.Long:      return str ?? lng.ToString();
                default:                return "None";
            }
        }
        
        public long AsLong() {
            if (type == KeyType.Long)
                return lng;
            throw new InvalidOperationException($"cannot return JsonKey as long. {ToString()}");
        }
    }
    
    public enum KeyType
    {
        None,
        String,
        Long
    }
    
    public class KeyComparer : IComparer<JsonKey>
    {
        public int Compare(JsonKey x, JsonKey y) {
            int dif = x.type - y.type;
            if (dif != 0)
                return dif;
            
            switch (x.type) {
                case KeyType.String:
                    return string.Compare(x.str, y.str, StringComparison.InvariantCulture);
                case KeyType.Long:
                    long longDif = x.lng - y.lng;
                    if (longDif < 0)
                        return -1;
                    if (longDif > 0)
                        return +1;
                    return 0;
                default:
                    throw new InvalidOperationException("Invalid IdType"); 
            }
        }
    }
    
    public class KeyEqualityComparer : IEqualityComparer<JsonKey>
    {
        public bool Equals(JsonKey x, JsonKey y) {
            if (x.type != y.type)
                return false;
            
            switch (x.type) {
                case KeyType.String: return x.str == y.str;
                case KeyType.Long:   return x.lng == y.lng;
                default:
                    throw new InvalidOperationException("Invalid IdType"); 
            }
        }

        public int GetHashCode(JsonKey jsonKey) {
            switch (jsonKey.type) {
                case KeyType.String: return jsonKey.str.GetHashCode();
                case KeyType.Long:   return jsonKey.lng.GetHashCode();
                default:
                    throw new InvalidOperationException("Invalid IdType"); 
            }
        }
    }
}