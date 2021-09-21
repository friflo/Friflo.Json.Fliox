// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Val;

namespace Friflo.Json.Fliox.DB.Graph.Internal.Map
{
    internal class JsonEntitiesMatcher : ITypeMatcher {
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(JsonEntities))
                return null;
            return new JsonEntitiesMapper (config, type);
        }
    }

    internal class JsonEntitiesMapper : TypeMapper<JsonEntities>
    {
        private readonly    KeyMapper<JsonKey>  keyMapper;
        private             JsonValueMapper     jsonValueMapper;

        public override string DataTypeName() { return "JsonEntities"; }

        public JsonEntitiesMapper(StoreConfig config, Type type) : base (config, type, false, false) {
            keyMapper       = (KeyMapper<JsonKey>)config.GetKeyMapper(typeof(JsonKey));
        }

        public override void InitTypeMapper(TypeStore typeStore) {
            jsonValueMapper = (JsonValueMapper)typeStore.GetTypeMapper(typeof(JsonValue));
        }

        public override void Write(ref Writer writer, JsonEntities value) {
            int startLevel = writer.IncLevel();
            writer.bytes.AppendChar('{');
            int n = 0;
            foreach (var entity in value.entities) {
                JsonKey     key     = entity.Key;
                JsonUtf8    json    = entity.Value.Json;
                writer.WriteKey(keyMapper, key, n++);
                if (!json.IsNull())
                    writer.bytes.AppendArray(json);
                else
                    writer.AppendNull(); // todo throw exception. value.json must be not null
            }
            if (writer.pretty)
                writer.IndentEnd();
            writer.bytes.AppendChar('}');
            writer.DecLevel(startLevel);
        }

        public override JsonEntities Read(ref Reader reader, JsonEntities slot, out bool success) {
            if (!reader.StartObject(this, out success))
                return default;

            if (slot.entities == null) {
                slot.Init(0);
            } else {
                slot.entities.Clear();
            }
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ValueNull:
                        JsonKey key = keyMapper.ReadKey(ref reader, out success);
                        if (!success)
                            return default;
                        JsonValue jsonValue = jsonValueMapper.Read(ref reader, new JsonValue(), out success);
                        if (!success)
                            return default;
                        slot.entities[key] = new EntityValue(jsonValue.json);
                        break;
                    case JsonEvent.ObjectEnd:
                        success = true;
                        return slot;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return reader.ErrorMsg<JsonEntities>("unexpected state: ", ev, out success);
                }
            }
        }
    }
}