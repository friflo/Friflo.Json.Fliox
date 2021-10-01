// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Mapper.Map.Key
{
    internal sealed class StringKeyMapper : KeyMapper<string>
    {
        public override void WriteKey (ref Writer writer, in string key) {
            writer.WriteString(key);
        }
        
        public override string ReadKey (ref Reader reader, out bool success) {
            success = true;
            return reader.parser.key.AsString();
        }
        
        public override JsonKey     ToJsonKey      (in string key) {
            return new JsonKey(key);
        }
        
        public override string      ToKey          (in JsonKey key) {
            return key.AsString();
        }
    }
}