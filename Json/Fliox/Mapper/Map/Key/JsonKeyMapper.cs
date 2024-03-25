// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Mapper.Map.Key
{
    internal sealed class JsonKeyMapper : KeyMapper<JsonKey>
    {
        public override void WriteKey (ref Writer writer, in JsonKey key) {
            var obj = key.keyObj;
            if (obj == JsonKey.LONG) {
                // writer.bytes.AppendChar('\"');
                writer.format.AppendLong(ref writer.bytes, key.lng);
                // writer.bytes.AppendChar('\"');
                return;
            }
            if (obj is string) {
                writer.WriteJsonKey(key);
                return;
            }
            if (obj == JsonKey.GUID) {
                writer.WriteGuid(key.Guid);
                return;
            }
            throw new InvalidOperationException($"cannot write JsonKey: {key}");
        }
        
        public override JsonKey ReadKey (ref Reader reader, out bool success) {
            ref var parser = ref reader.parser;
            success = true;
            return new JsonKey(parser.key, default);
        }
        
        public override JsonKey     ToJsonKey      (in JsonKey key) {
            return key;
        }
        
        public override JsonKey      ToKey          (in JsonKey key) {
            return key;
        }
    }
}