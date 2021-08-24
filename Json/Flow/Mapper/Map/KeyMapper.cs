// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Flow.Mapper.Map
{
    public class KeyMapper
    {
        private static readonly StringKeyMapper StringKeyMapper = new StringKeyMapper();
            
        public static KeyMapper GetKeyMapper<TKey>() {
            var keyType = typeof(TKey);
            if (keyType == typeof(string))
                return StringKeyMapper;
            throw new InvalidOperationException($"unsupported key Type: {keyType.FullName}");
        }
    }
    
    public abstract class KeyMapper<TKey> : KeyMapper
    {
        public abstract void        WriteKey       (ref Writer writer, TKey key);
        public abstract TKey        ReadKey        (ref Reader reader, out bool success);
    }
    
    public class StringKeyMapper : KeyMapper<string>
    {
        public override void WriteKey (ref Writer writer, string key) {
            writer.WriteString(key);
        }
        
        public override string ReadKey (ref Reader reader, out bool success) {
            success = true;
            return reader.parser.key.ToString();
        }
    }

    
    
}