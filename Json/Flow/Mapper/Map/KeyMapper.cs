// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map.Key;

namespace Friflo.Json.Flow.Mapper.Map
{
    public class KeyMapper
    {
        internal static Dictionary<Type, KeyMapper> CreateDefaultKeyMappers() {
            var keyMappers = new Dictionary<Type, KeyMapper> {
                { typeof(string),   new StringKeyMapper() },
                { typeof(long),     new LongKeyMapper()   },
                { typeof(int),      new IntKeyMapper()    },
                { typeof(short),    new ShortKeyMapper()  },
                { typeof(byte),     new ByteKeyMapper()   },
                { typeof(JsonKey),  new JsonKeyMapper()   },
                { typeof(Guid),     new GuidKeyMapper()   },
            };
            return keyMappers;
        }
    }
    
    public abstract class KeyMapper<TKey> : KeyMapper
    {
        public abstract void        WriteKey       (ref Writer writer, in TKey key);
        public abstract TKey        ReadKey        (ref Reader reader, out bool success);
        public abstract JsonKey     ToJsonKey      (in TKey key);
        public abstract TKey        ToKey          (in JsonKey key);
    }
}