// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    internal sealed class JsonEntityMatcher : ITypeMatcher {
        public static readonly JsonEntityMatcher Instance = new JsonEntityMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(JsonEntity))
                return null;
            return new JsonEntityMapper (config, type);
        }
    }
    
    /// <summary>
    /// Basically same implementation as <see cref="JsonValueMapper"/>
    /// Note: Keep both implementation in sync
    /// </summary>
    public sealed class JsonEntityMapper : TypeMapper<JsonEntity>
    {
        public override string  DataTypeName()                  => "JsonEntity";
        public override bool    IsNull(ref JsonEntity value)    => value.value.IsNull();

        public JsonEntityMapper(StoreConfig config, Type type) : base (config, type, false, false) { }
        
        public override bool IsNullVar(in Var value) {
            var jsonValue = (JsonEntity)value.TryGetObject();
            return jsonValue.value.IsNull();
        }

        public override void Write(ref Writer writer, JsonEntity value) {
            if (!value.value.IsNull())
                writer.bytes.AppendArray(value.value);
            else
                writer.AppendNull();
        }

        public override JsonEntity Read(ref Reader reader, JsonEntity slot, out bool success) {
            var stub = reader.jsonWriterStub;
            if (stub == null)
                reader.jsonWriterStub = stub = new Utf8JsonWriterStub();
            
            ref var serializer = ref stub.jsonWriter;
            serializer.InitSerializer();
            serializer.WriteTree(ref reader.parser);
            success     = true;
            var pool    = reader.readerPool;
            if (pool != null) {
                return new JsonEntity(pool.CreateJsonValue(serializer.json));
            }
            var json = serializer.json.AsArray();
            return new JsonEntity (new JsonValue(json));
        }
    }
}