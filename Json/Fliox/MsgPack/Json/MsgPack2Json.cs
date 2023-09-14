// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using static Friflo.Json.Fliox.MsgPack.MsgFormat;

namespace Friflo.Json.Fliox.MsgPack.Json
{
    public struct MsgPack2Json
    {
        private Utf8JsonWriter jsonWriter;
        
        public JsonValue ToJson(ReadOnlySpan<byte> msg)
        {
            jsonWriter.InitSerializer();
            var msgReader = new MsgReader(msg);
            WriteElement(ref msgReader);
            return new JsonValue(jsonWriter.json);
        }
        
        private void WriteElement(ref MsgReader msgReader)
        {
            var type = msgReader.NextMsg();
            switch (type)
            {
                case nil:
                    jsonWriter.ElementNul();
                    break;
                case <= fixintPosMax:
                case >= fixintNeg:
                case    uint8:
                case    uint16:
                case    uint32:
                case    uint64:
                case    int8:
                case    int16:
                case    int32:
                case    int64: {
                    var value = msgReader.ReadInt64();
                    jsonWriter.ElementLng(value);
                    break;
                }
                case    float32: {
                    var value = msgReader.ReadFloat32();
                    jsonWriter.ElementDbl(value);
                    break;
                }
                case    float64: {
                    var value = msgReader.ReadFloat64();
                    jsonWriter.ElementDbl(value);
                    break;
                }
                case >= fixstr and <= fixstrMax:
                case    str8:
                case    str16:
                case    str32: {
                    var value = msgReader.ReadString();
                    jsonWriter.ElementStr(value);
                    break;
                }
                case >= fixmap and <= fixmapMax:
                case    map16:
                case    map32:
                    jsonWriter.ObjectStart();
                    WriteObject(ref msgReader);
                    jsonWriter.ObjectEnd();
                    break;
                case >= fixarray and <= fixarrayMax:
                case    array16:
                case    array32:
                    jsonWriter.ArrayStart(false);
                    WriteArray(ref msgReader);
                    jsonWriter.ArrayEnd();
                    break;
            }
        }
        
        private void WriteObject(ref MsgReader msgReader)
        {
            if (!msgReader.ReadObject(out int length)) {
                return;
            }
            for (int n = 0; n < length; n++)
            {
                // --- read key
                ReadOnlySpan<byte> key;
                var keyType = msgReader.NextMsg();
                switch (keyType) {
                    case >= fixstr and <= fixstrMax:
                    case    str8:
                    case    str16:
                    case    str32: {
                        key = msgReader.ReadStringSpan();
                        break;
                    }
                    default:
                        return;
                }
                // --- read value
                var valueType = msgReader.NextMsg();
                switch (valueType)
                {
                    case <= fixintPosMax:
                    case >= fixintNeg:
                    //
                    case    uint8:
                    case    uint16:
                    case    uint32:
                    case    uint64:
                    //
                    case    int8:
                    case    int16:
                    case    int32:
                    case    int64: {
                        var value = msgReader.ReadInt64();
                        jsonWriter.MemberLng(key, value);
                        break;
                    }
                    case    float32: {
                        var value = msgReader.ReadFloat32();
                        jsonWriter.MemberDbl(key, value);
                        break;
                    }
                    case    float64: {
                        var value = msgReader.ReadFloat64();
                        jsonWriter.MemberDbl(key, value);
                        break;
                    }
                    case >= fixstr and <= fixstrMax:
                    case    str8:
                    case    str16:
                    case    str32: {
                        var value = msgReader.ReadStringSpan();
                        jsonWriter.MemberStr(key, value);
                        break;
                    }
                }
            }
        }
        
        private void WriteArray(ref MsgReader msgReader)
        {
            if (!msgReader.ReadArray(out int length)) {
                return;
            }
            for (int n = 0; n < length; n++) {
                WriteElement(ref msgReader);   
            }
        }
    }
}
