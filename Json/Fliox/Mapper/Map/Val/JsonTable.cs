// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    // ------------------------- PatchValueMatcher / PatchValueMapper -------------------------
    internal sealed class JsonTableMatcher : ITypeMatcher {
        public static readonly JsonTableMatcher Instance = new JsonTableMatcher();
        
        internal static readonly Bytes True     = new Bytes("true");
        internal static readonly Bytes False    = new Bytes("false");
        internal static readonly Bytes Null     = new Bytes("null");
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(JsonTable))
                return null;
            return new JsonTableMapper (config, type);
        }
    }
    
    internal sealed class JsonTableMapper : TypeMapper<JsonTable>
    {
        public override string  DataTypeName()              => "JsonTable";
        public override bool    IsNull(ref JsonTable value) => value == null;

        private static readonly Bytes NewRow = new Bytes("],\n[");

        public JsonTableMapper(StoreConfig config, Type type) : base (config, type, true, false) { }
        
        private static void WriteItems(ref Writer writer, JsonTable array)
        {
            int     pos         = 0;
            var     itemType    = array.GetItemType(pos, out int next);
            if (itemType == JsonItemType.End) {
                return;
            }
            bool    isFirstItem = true;
            ref var bytes       = ref writer.bytes;
            ref var format      = ref writer.format;
            bytes.AppendChar2('\n','[');
            
            while (true)
            {
                switch (itemType) {
                    case JsonItemType.Null:
                        bytes.AppendBytes(JsonTableMatcher.Null);    
                        break;
                    case JsonItemType.True:
                    case JsonItemType.False: {
                        var value = array.ReadBool(pos);
                        if (value) {
                            bytes.AppendBytes(JsonTableMatcher.True);
                        } else {
                            bytes.AppendBytes(JsonTableMatcher.False);
                        }
                        break;
                    }
                    case JsonItemType.Uint8: {
                        var value = array.ReadUint8(pos);
                        format.AppendLong(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Int16: {
                        var value = array.ReadInt16(pos);
                        format.AppendLong(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Int32: {
                        var value = array.ReadInt32(pos);
                        format.AppendLong(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Int64: {
                        var value = array.ReadInt64(pos);
                        format.AppendLong(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Flt32: {
                        var value = array.ReadFlt32(pos);
                        format.AppendFlt(ref bytes, value);
                        break;
                    }
                    case JsonItemType.Flt64: {
                        var value = array.ReadFlt64(pos);
                        format.AppendDbl(ref bytes, value);
                        break;
                    }
                    case JsonItemType.JSON: {
                        var value = array.ReadBytes(pos);
                        bytes.AppendBytes(value);
                        break;
                    }
                    case JsonItemType.ByteString: {
                        var value = array.ReadBytes(pos);
                        Utf8JsonWriter.AppendEscStringBytes(ref bytes, value.AsSpan());
                        break;
                    }
                    case JsonItemType.CharString: {
                        var value = array.ReadCharSpan(pos);
                        Utf8JsonWriter.AppendEscString(ref bytes, value);
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
                    case JsonItemType.NewRow:
                        pos         = next;
                        itemType    = array.GetItemType(pos, out next);
                        if (!isFirstItem) {
                            bytes.end--; // remove last terminator
                        }
                        if (itemType == JsonItemType.End) {
                            bytes.AppendChar2(']', '\n');
                            return;
                        }
                        writer.bytes.AppendBytes(NewRow);
                        isFirstItem = true;
                        continue;
                    case JsonItemType.End:
                        if (!isFirstItem) {
                            bytes.end--; // remove last terminator
                        }
                        bytes.AppendChar2(']', '\n');
                        return;
                    default:
                        throw new InvalidComObjectException($"unexpected itemType: {itemType}");
                }
                bytes.AppendChar(',');
                isFirstItem = false;
                pos         = next;
                itemType    = array.GetItemType(pos, out next);
            }
        }
        
        public override void Write(ref Writer writer, JsonTable value)
        {
            int startLevel = writer.IncLevel();
            writer.WriteArrayBegin();
                
            WriteItems(ref writer, value);

            writer.WriteArrayEnd();
            writer.DecLevel(startLevel);
        }
        
        private bool StartArray(ref Reader reader, out bool success) {
            ref var parser = ref reader.parser;
            var ev = parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    reader.ErrorIncompatible<JsonTable>(DataTypeName(), this, out success);
                    return default;
                case JsonEvent.ArrayStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    reader.ErrorIncompatible<JsonTable>(DataTypeName(), this, out success);
                    return false;
            }
        }

        public override JsonTable Read(ref Reader reader, JsonTable value, out bool success)
        {
            if (!StartArray(ref reader, out success)) {
                return default;
            }
            value ??= new JsonTable();
            ref var parser = ref reader.parser;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNull:
                        return reader.ErrorIncompatible<JsonTable>(DataTypeName(), this, out success);
                    case JsonEvent.ArrayStart:
                        if (!ReadItems(ref reader, value, out success)) {
                            return null;
                        }
                        continue;
                    case JsonEvent.ArrayEnd:
                        return value;
                    default:
                        success = false;
                        reader.ErrorIncompatible<JsonTable>(DataTypeName(), this, out success);
                        return null;
                }
            }
        }
        
        private static bool ReadItems(ref Reader reader, JsonTable value, out bool success)
        {
            ref var parser = ref reader.parser;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString: {
                        var span = parser.value.AsSpan();
                        var len  = span.Length;
                        if (len == Bytes.GuidLength && Bytes.TryParseGuid(span, out var guid)) {
                            value.WriteGuid(guid);
                            break;
                        }
                        if (Bytes.TryParseDateTime(span, out var dateTime)) {
                            value.WriteDateTime(dateTime);
                            break;
                        }
                        value.WriteByteString(parser.value.AsSpan());
                        break;
                    }
                    case JsonEvent.ValueNumber: {
                        var span = parser.value.AsSpan();
                        if (!parser.isFloat) {
                            var lng = ValueParser.ParseLong(span, ref reader.strBuf, out success);
                            if (!success) {
                                return reader.ErrorMsg<bool>("invalid integer: ", parser.value, out success);
                            }
                            value.WriteInt64(lng);
                            break;
                        }
                        var dbl = ValueParser.ParseDouble(span, ref reader.strBuf, out success);
                        if (!success) {
                            return reader.ErrorMsg<bool>("invalid floating point number: ", parser.value, out success);
                        }
                        var exponent    = Math.Log(dbl, 10);
                        // max float: 3.40282346638528859e+38. Is exponent is > 38? => Write as double
                        if (exponent >= 39) {
                            value.WriteFlt64(dbl);
                            break;
                        }
                        // More than 8 decimal digit precision? => Write as double
                        if (DigitCount(span) > 8) {
                            value.WriteFlt64(dbl);
                            break;
                        }
                        value.WriteFlt32((float)dbl);
                        break;
                    }
                    case JsonEvent.ValueBool:
                        value.WriteBoolean(parser.boolValue);
                        break;
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart: {
                        var start   = parser.Position - 1;
                        parser.SkipTree();
                        var end     = parser.Position;
                        var json    = parser.GetInputSpan(start, end);
                        value.WriteJSON(json);
                        break;
                    }
                    case JsonEvent.ValueNull:
                        value.WriteNull();
                        break;
                    case JsonEvent.ArrayEnd:
                        value.WriteNewRow();
                        success = true;
                        return true;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return reader.ErrorMsg<bool>("unexpected state: ", ev, out success);
                }
            }
        }
        
        private static int DigitCount(in ReadOnlySpan<byte> span) {
            int len         = span.Length;
            int digitCount  = 0;
            for (int n = 0; n < len; n++) {
                var c = span[n];
                if ('0' <= c && c <= '9') {
                    digitCount++;
                    continue;
                }
                if (c == 'e' || c == 'E') {
                    return digitCount;
                }
            }
            return digitCount;
        }
    }
}