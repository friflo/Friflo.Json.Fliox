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
        
        public JsonKey (string str) {
            this.type   = KeyType.String;
            this.str    = str;
            this.lng    = 0;
        }
        
        public JsonKey (long lng) {
            this.type   = KeyType.Long;
            this.str    = null;
            this.lng    = lng;
        }
        
        public string AsString() {
            switch (type) {
                case KeyType.String:    return str;
                case KeyType.Long:      return lng.ToString();
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
    
    internal readonly struct KeyDictionary<T>
    {
        private readonly Dictionary<JsonKey, T> dict;
            
        internal KeyDictionary(Dictionary<JsonKey, T> dict = null) {
            if (dict == null) {
                dict = new Dictionary<JsonKey, T>(KeyEqualityComparer.Comparer);
            }
            this.dict = dict;
        }
    }
    
    internal class KeyEqualityComparer : IEqualityComparer<JsonKey>
    {
        internal static readonly KeyEqualityComparer Comparer = new KeyEqualityComparer();
        
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