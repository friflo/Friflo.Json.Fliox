// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Mapper.Map.Val
{
    public class JsonValue
    {
        public string       json;
    }
    
    // ------------------------- PatchValueMatcher / PatchValueMapper -------------------------
    public class PatchValueMatcher : ITypeMatcher {
        public static readonly PatchValueMatcher Instance = new PatchValueMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(JsonValue))
                return null;
            return new PatchValueMapper (config, type);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PatchValueMapper : TypeMapper<JsonValue>
    {
        public override string DataTypeName() { return "PatchValue"; }

        public PatchValueMapper(StoreConfig config, Type type) : base (config, type, true, false) { }

        public override void Write(ref Writer writer, JsonValue value) {
            writer.bytes.AppendString(value.json);
        }

        public override JsonValue Read(ref Reader reader, JsonValue slot, out bool success) {
            var stub = reader.jsonSerializerStub;
            if (stub == null)
                reader.jsonSerializerStub = stub = new JsonSerializerStub();
            
            ref var serializer = ref stub.jsonSerializer;
            serializer.InitSerializer();
            serializer.WriteTree(ref reader.parser);
            var json = serializer.json.ToString();
            var patchValue = new JsonValue { json = json };
            success = true;
            return patchValue;
        }
    }
}