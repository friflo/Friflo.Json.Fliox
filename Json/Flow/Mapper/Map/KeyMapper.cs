// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Mapper.Map.Key;

namespace Friflo.Json.Flow.Mapper.Map
{
    public class KeyMapper
    {
        private static readonly StringKeyMapper StringKeyMapper = new StringKeyMapper();
        private static readonly LongKeyMapper   LongKeyMapper   = new LongKeyMapper();
            
        public static KeyMapper GetKeyMapper<TKey>() {
            var keyType = typeof(TKey);
            if (keyType == typeof(string))
                return StringKeyMapper;
            if (keyType == typeof(long))
                return LongKeyMapper;
            throw new InvalidOperationException($"unsupported key Type: {keyType.FullName}");
        }
    }
    
    public abstract class KeyMapper<TKey> : KeyMapper
    {
        public abstract void        WriteKey       (ref Writer writer, TKey key);
        public abstract TKey        ReadKey        (ref Reader reader, out bool success);
        public abstract JsonKey     ToJsonKey      (TKey key);
        public abstract TKey        ToKey          (JsonKey key);
    }
}