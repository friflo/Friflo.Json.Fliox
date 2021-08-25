// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Flow.Mapper.Map.Key
{
    public class GuidKeyMapper : KeyMapper<Guid>
    {
        public override void WriteKey (ref Writer writer, in Guid key) {
            writer.WriteString(key.ToString());
        }
        
        public override Guid ReadKey (ref Reader reader, out bool success) {
            if (Guid.TryParse(reader.parser.key.ToString(), out var result)) {
                success = true;
                return result;
            }
            success = false;
            return default;
        }
        
        public override JsonKey     ToJsonKey      (in Guid key) {
            return new JsonKey(key.ToString());
        }
        
        public override Guid      ToKey          (in JsonKey key) {
            return new Guid(key.AsString());
        }
    }
}