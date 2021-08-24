// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Mapper.Map.Key
{
    public class StringKeyMapper : KeyMapper<string>
    {
        public override void WriteKey (ref Writer writer, string key) {
            writer.WriteString(key);
        }
        
        public override string ReadKey (ref Reader reader, out bool success) {
            success = true;
            return reader.parser.key.ToString();
        }
        
        public override JsonKey     ToJsonKey      (string key) {
            return new JsonKey(key);
        }
        
        public override string      ToKey          (in JsonKey key) {
            return key.AsString();
        }
    }
}