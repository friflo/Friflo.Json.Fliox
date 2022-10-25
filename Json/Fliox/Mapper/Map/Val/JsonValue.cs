// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    // ------------------------- PatchValueMatcher / PatchValueMapper -------------------------
    internal sealed class JsonValueMatcher : ITypeMatcher {
        public static readonly JsonValueMatcher Instance = new JsonValueMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(JsonValue))
                return null;
            return new JsonValueMapper (config, type);
        }
    }
    
    /// <summary>
    /// Basically same implementation as <see cref="JsonEntityMapper"/>
    /// Note: Keep both implementation in sync
    /// </summary>
    public sealed class JsonValueMapper : TypeMapper<JsonValue>
    {
        public override string DataTypeName() { return "JsonValue"; }

        public JsonValueMapper(StoreConfig config, Type type) : base (config, type, false, false) { }
        
        public override bool IsNullVar(in Var value) {
            var jsonValue = (JsonValue)value.TryGetObject();
            return jsonValue.IsNull();
        }

        public override void Write(ref Writer writer, JsonValue value) {
            if (!value.IsNull())
                writer.bytes.AppendArray(value);
            else
                writer.AppendNull();
        }

        public override JsonValue Read(ref Reader reader, JsonValue slot, out bool success) {
            var stub = reader.jsonWriterStub;
            if (stub == null)
                reader.jsonWriterStub = stub = new Utf8JsonWriterStub();
            
            ref var serializer = ref stub.jsonWriter;
            serializer.InitSerializer();
            serializer.WriteTree(ref reader.parser);
            var json = serializer.json.AsArray();
            var patchValue = new JsonValue (json);
            success = true;
            return patchValue;
        }
    }
}