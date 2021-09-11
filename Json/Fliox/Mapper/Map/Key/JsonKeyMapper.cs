// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Mapper.Map.Key
{
    internal class JsonKeyMapper : KeyMapper<JsonKey>
    {
        public override void WriteKey (ref Writer writer, in JsonKey key) {
            switch (key.type) {
                case JsonKeyType.Long:
                    writer.bytes.AppendChar('\"');
                    writer.format.AppendLong(ref writer.bytes, key.lng);
                    writer.bytes.AppendChar('\"');
                    break;
                case JsonKeyType.String:
                    writer.WriteString(key.str);
                    break;
                case JsonKeyType.Guid:
                    writer.WriteGuid(key.guid);
                    break;
                default:
                    throw new InvalidOperationException($"cannot write JsonKey: {key}");
            }
        }
        
        public override JsonKey ReadKey (ref Reader reader, out bool success) {
            ref var parser = ref reader.parser;
            success = true;
            return new JsonKey(ref parser.key, ref parser.valueParser);
        }
        
        public override JsonKey     ToJsonKey      (in JsonKey key) {
            return key;
        }
        
        public override JsonKey      ToKey          (in JsonKey key) {
            return key;
        }
    }
}