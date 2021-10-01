// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Mapper.Map.Key
{
    internal sealed class GuidKeyMapper : KeyMapper<Guid>
    {
        public override void WriteKey (ref Writer writer, in Guid key) {
            writer.WriteGuid (key);
        }
        
        public override Guid ReadKey (ref Reader reader, out bool success) {
            if (reader.parser.key.TryParseGuid(out var result, out _)) {
                success = true;
                return result;
            }
            success = false;
            return default;
        }
        
        public override JsonKey     ToJsonKey      (in Guid key) {
            return new JsonKey(key);
        }
        
        public override Guid      ToKey          (in JsonKey key) {
            return key.AsGuid();
        }
    }
}