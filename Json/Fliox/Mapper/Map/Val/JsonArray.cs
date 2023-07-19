// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    // ------------------------- PatchValueMatcher / PatchValueMapper -------------------------
    internal sealed class JsonArrayMatcher : ITypeMatcher {
        public static readonly JsonArrayMatcher Instance = new JsonArrayMatcher();
        
        internal static readonly Bytes True     = new Bytes("true");
        internal static readonly Bytes False    = new Bytes("false");
        internal static readonly Bytes Null     = new Bytes("null");
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(JsonArray))
                return null;
            return new JsonArrayMapper (config, type);
        }
    }
    
    internal sealed class JsonArrayMapper : TypeMapper<JsonArray>
    {
        public override string  DataTypeName()              => "JsonArray";
        public override bool    IsNull(ref JsonArray value) => value == null;
        

        public JsonArrayMapper(StoreConfig config, Type type) : base (config, type, true, false) { }
        
        private static void WriteItems(ref Writer writer, JsonArray array) {
            int     pos         = 0;
            bool    isFirstItem = true;
            ref var bytes       = ref writer.bytes;
            
            while (true)
            {
                var itemType = array.GetItemType(pos, out int next);
                switch (itemType) {
                    case JsonItemType.Null:
                        bytes.AppendBytes(JsonArrayMatcher.Null);    
                        break;
                    case JsonItemType.True:
                    case JsonItemType.False: {
                        var value = array.ReadBool(pos);
                        if (value) {
                            bytes.AppendBytes(JsonArrayMatcher.True);
                        } else {
                            bytes.AppendBytes(JsonArrayMatcher.False);
                        }
                        break;
                    }
                    case JsonItemType.Uint8: {
                        var value = array.ReadUint8(pos);
                        writer.format.AppendLong(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Int16: {
                        var value = array.ReadInt16(pos);
                        writer.format.AppendLong(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Int32: {
                        var value = array.ReadInt32(pos);
                        writer.format.AppendLong(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Int64: {
                        var value = array.ReadInt64(pos);
                        writer.format.AppendLong(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Flt32: {
                        var value = array.ReadFlt32(pos);
                        writer.format.AppendFlt(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Flt64: {
                        var value = array.ReadFlt64(pos);
                        writer.format.AppendFlt(ref bytes, value);
                        break;
                    }
                    case JsonItemType.ByteString: {
                        var value = array.ReadBytes(pos);
                        bytes.AppendChar('"');
                        bytes.AppendString("bytes");
                        bytes.AppendChar('"');
                        break;
                    }
                    case JsonItemType.CharString: {
                        bytes.AppendChar('"');
                        bytes.AppendString("chars");
                        bytes.AppendChar('"');
                        break;
                    }
                    case JsonItemType.DateTime: {
                        var value = array.ReadDateTime(pos);
                        bytes.AppendChar('"');
                        bytes.AppendDateTime(value, writer.charBuf);
                        bytes.AppendChar('"');
                        break;
                    }
                    case JsonItemType.Guid: {
                        var value = array.ReadGuid(pos);
                        bytes.AppendChar('"');
                        bytes.AppendGuid(value);
                        bytes.AppendChar('"');
                        break;
                    }
                    case JsonItemType.End:
                        if (!isFirstItem) {
                            bytes.end--; // remove last terminator
                        }
                        return;
                    default:
                        throw new InvalidComObjectException("unexpected itemType: {itemType}");
                }
                isFirstItem = false;
                bytes.AppendChar(',');
                pos = next;
            }
        }
        
        public override void Write(ref Writer writer, JsonArray array) {
            int startLevel = writer.IncLevel();
            writer.WriteArrayBegin();
            
            WriteItems(ref writer, array);
            
            writer.WriteArrayEnd();
            writer.DecLevel(startLevel);
        }

        public override JsonArray Read(ref Reader reader, JsonArray value, out bool success) {
            success = false;
            return null;
            /* ref var parser = ref reader.parser;
            var ev = parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    success = true;
                    return new JsonKey();
                case JsonEvent.ValueString:
                    success = true;
                    return new JsonKey(parser.value.AsSpan());
                case JsonEvent.ValueNumber:
                    success = true;
                    return new JsonKey(parser.value, value);
                default:
                    return reader.ErrorMsg<JsonKey>("Expect string as JsonKey. ", ev, out success);
            } */
        }
    }
}