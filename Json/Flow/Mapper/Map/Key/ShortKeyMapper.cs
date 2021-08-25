// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Mapper.Map.Key
{
    public class ShortKeyMapper : KeyMapper<short>
    {
        public override void WriteKey (ref Writer writer, in short key) {
            writer.bytes.AppendChar('\"');
            writer.format.AppendLong(ref writer.bytes, key);
            writer.bytes.AppendChar('\"');
        }
        
        public override short ReadKey (ref Reader reader, out bool success) {
            ref var parser = ref reader.parser;
            return (short)parser.valueParser.ParseInt(ref parser.key, ref parser.errVal, out success);
        }
        
        public override JsonKey     ToJsonKey      (in short key) {
            return new JsonKey(key);
        }
        
        public override short        ToKey          (in JsonKey key) {
            return (short)key.AsLong();
        }
    }
}