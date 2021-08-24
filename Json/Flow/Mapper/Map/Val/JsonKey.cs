// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Mapper.Map.Val
{
    // ------------------------- PatchValueMatcher / PatchValueMapper -------------------------
    public class JsonKeyMatcher : ITypeMatcher {
        public static readonly JsonKeyMatcher Instance = new JsonKeyMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(JsonKey))
                return null;
            return new JsonKeyMapper (config, type);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class JsonKeyMapper : TypeMapper<JsonKey>
    {
        public override string DataTypeName() { return "JsonKey"; }

        public JsonKeyMapper(StoreConfig config, Type type) : base (config, type, false, false) { }

        public override void Write(ref Writer writer, JsonKey value) {
            switch (value.type) {
                case KeyType.Long:
                    writer.bytes.AppendChar('\"');
                    writer.format.AppendLong(ref writer.bytes, value.lng);
                    writer.bytes.AppendChar('\"');
                    break;
                case KeyType.String:
                    writer.WriteString(value.str);
                    break;
                case KeyType.None:
                    writer.AppendNull();
                    break;
            }
        }

        public override JsonKey Read(ref Reader reader, JsonKey slot, out bool success) {
            // var stub = reader.jsonSerializerStub;
            // if (stub == null)
            //     reader.jsonSerializerStub = stub = new JsonSerializerStub();
            ref var parser = ref reader.parser;
            var ev = parser.Event;
            switch (ev) {
                case JsonEvent.ValueString:
                    success = true;
                    if (parser.value.IsIntegral()) {
                        var lng = parser.ValueAsLong(out success);
                        return new JsonKey(lng);
                    }
                    return new JsonKey(parser.value.ToString());
                default:
                    throw new InvalidOperationException($"JsonKey - unexpected event: {ev}");
            }
        }
    }
}