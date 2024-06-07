// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    // ------------------------- PatchValueMatcher / PatchValueMapper -------------------------
    internal sealed class JsonKeyMatcher : ITypeMatcher {
        public static readonly JsonKeyMatcher Instance = new JsonKeyMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(JsonKey))
                return null;
            return new JsonKeyMapper (config, type);
        }
    }
    
    internal sealed class JsonKeyMapper : TypeMapper<JsonKey>
    {
        public override StandardTypeId  StandardTypeId              => StandardTypeId.JsonKey;
        public override string          StandardTypeName            => "JsonKey";
        public override string          DataTypeName()              => "JsonKey";
        public override bool            IsNull(ref JsonKey value)   => value.IsNull();

        public JsonKeyMapper(StoreConfig config, Type type) : base (config, type, true, false) { }
        
        public override bool IsNullVar(in Var value) {
            var key = (JsonKey)value.TryGetObject();
            return key.IsNull();
        }

        public override void Write(ref Writer writer, JsonKey value) {
            var obj = value.keyObj;
            if (obj == JsonKey.LONG) {
                // writer.bytes.AppendChar('\"');
                writer.format.AppendLong(ref writer.bytes, value.lng);
                // writer.bytes.AppendChar('\"');
                return;
            }
            if (obj is string) {
                writer.WriteJsonKey(value);
                return;
            }
            if (obj == JsonKey.GUID) {
                writer.WriteGuid(value.Guid);
                return;
            }
            if (obj == null) {
                writer.AppendNull();
                return;
            }
        }

        public override JsonKey Read(ref Reader reader, JsonKey value, out bool success) {
            ref var parser = ref reader.parser;
            var ev = parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    success = true;
                    return new JsonKey();
                case JsonEvent.ValueString:
                    success = true;
                    return new JsonKey(parser.value.AsSpan(), default);
                case JsonEvent.ValueNumber:
                    success = true;
                    return new JsonKey(parser.value, value);
                default:
                    return reader.ErrorMsg<JsonKey>("Expect string as JsonKey. ", ev, out success);
            }
        }
    }
}