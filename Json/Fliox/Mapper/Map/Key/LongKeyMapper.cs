// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Key
{
    internal sealed class LongKeyMapper : KeyMapper<long>
    {
        public override void WriteKey (ref Writer writer, in long key) {
            writer.bytes.AppendChar('\"');
            writer.format.AppendLong(ref writer.bytes, key);
            writer.bytes.AppendChar('\"');
        }
        
        public override long ReadKey (ref Reader reader, out bool success) {
            ref var parser = ref reader.parser;
            return ValueParser.ParseLong(parser.key.AsSpan(), ref parser.errVal, out success);
        }
        
        public override JsonKey     ToJsonKey      (in long key) {
            return new JsonKey(key);
        }
        
        public override long        ToKey          (in JsonKey key) {
            return key.AsLong();
        }
    }
}