// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Mapper.Map.Key
{
    internal sealed class IntKeyMapper : KeyMapper<int>
    {
        public override void WriteKey (ref Writer writer, in int key) {
            writer.bytes.AppendChar('\"');
            writer.format.AppendLong(ref writer.bytes, key);
            writer.bytes.AppendChar('\"');
        }
        
        public override int ReadKey (ref Reader reader, out bool success) {
            ref var parser = ref reader.parser;
            return parser.valueParser.ParseInt(ref parser.key, ref parser.errVal, out success);
        }
        
        public override JsonKey     ToJsonKey      (in int key) {
            return new JsonKey(key);
        }
        
        public override int        ToKey          (in JsonKey key) {
            return (int)key.AsLong();
        }
    }
}