// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using static Friflo.Json.Fliox.MsgPack.MsgFormat;

namespace Friflo.Json.Fliox.MsgPack.Json
{
    public class MsgPack2Json
    {
        private     Utf8JsonWriter      jsonWriter;
        private     string              error;
        private     MsgReaderState      readerState;
        
        public      string              Error       => error;
        public      MsgReaderState      ReaderState => readerState;
        
        public JsonValue ToJson(ReadOnlySpan<byte> msg)
        {
            jsonWriter.InitSerializer();
            var msgReader = new MsgReader(msg);
            
            Start(ref msgReader);
            
            readerState = msgReader.State;
            if (msgReader.State != MsgReaderState.Ok) {
                error = msgReader.Error;
                return default;
            }
            return new JsonValue(jsonWriter.json);
        }
        
        private void Start(ref MsgReader msgReader)
        {
            var data = msgReader.Data;
            if (msgReader.Pos >= data.Length) {
                msgReader.SetEofError();
                return;
            }
            var type = (MsgFormat)data[msgReader.Pos];
            TraverseElement(type, ref msgReader);
        }
        
        private void TraverseElement(MsgFormat type, ref MsgReader msgReader)
        {
            switch (type)
            {
                case nil:
                    jsonWriter.ElementNul();
                    return;
                case True:
                    jsonWriter.ElementBln(true);
                    return;
                case False:
                    jsonWriter.ElementBln(false);
                    return;
                case bin8:
                case bin16:
                case bin32:
                    WriteBinElement(ref msgReader);
                    return;
                case >= fixintPos and <= fixintPosMax:
                case >= fixintNeg and <= fixintNegMax:
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
                    jsonWriter.ElementLng(value);
                    return;
                }
                case    float32: {
                    var value = msgReader.ReadFloat32();
                    jsonWriter.ElementDbl(value);
                    return;
                }
                case    float64: {
                    var value = msgReader.ReadFloat64();
                    jsonWriter.ElementDbl(value);
                    return;
                }
                case >= fixstr and <= fixstrMax:
                case    str8:
                case    str16:
                case    str32: {
                    msgReader.ReadStringSpan(out var value);
                    jsonWriter.ElementStr(value);
                    return;
                }
                case >= fixmap and <= fixmapMax:
                case    map16:
                case    map32:
                    jsonWriter.ObjectStart();
                    TraverseObject(ref msgReader);
                    jsonWriter.ObjectEnd();
                    return;
                case >= fixarray and <= fixarrayMax:
                case    array16:
                case    array32:
                    jsonWriter.ArrayStart(false);
                    TraverseArray(ref msgReader);
                    jsonWriter.ArrayEnd();
                    return;
                default:
                    msgReader.SkipTree();
                    return;
            }
        }
        
        private void TraverseObject(ref MsgReader msgReader)
        {
            if (!msgReader.ReadObject(out int length)) {
                return;
            }
            var data = msgReader.Data;
            for (int n = 0; n < length; n++)
            {
                // --- read key
                if (msgReader.Pos >= data.Length) {
                    msgReader.SetEofError();
                    return;
                }
                msgReader.ReadKey(); // sets msgReader.KeyName

                // --- read value
                if (msgReader.Pos >= data.Length) {
                    msgReader.SetEofError();
                    return;
                }
                var valueType = (MsgFormat)data[msgReader.Pos];
                switch (valueType)
                {
                    case nil:
                        jsonWriter.MemberNul(msgReader.KeyName);
                        break;
                    case True:
                        jsonWriter.MemberBln(msgReader.KeyName, true);
                        break;
                    case False:
                        jsonWriter.MemberBln(msgReader.KeyName, false);
                        break;
                    case bin8:
                    case bin16:
                    case bin32:
                        WriteBinMember(ref msgReader);
                        break;
                    case >= fixintPos and <= fixintPosMax:
                    case >= fixintNeg and <= fixintNegMax:
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
                        jsonWriter.MemberLng(msgReader.KeyName, value);
                        break;
                    }
                    case    float32: {
                        var value = msgReader.ReadFloat32();
                        jsonWriter.MemberDbl(msgReader.KeyName, value);
                        break;
                    }
                    case    float64: {
                        var value = msgReader.ReadFloat64();
                        jsonWriter.MemberDbl(msgReader.KeyName, value);
                        break;
                    }
                    case >= fixstr and <= fixstrMax:
                    case    str8:
                    case    str16:
                    case    str32: {
                        msgReader.ReadStringSpan(out var value);
                        jsonWriter.MemberStr(msgReader.KeyName, value);
                        break;
                    } 
                }
            }
        }
        
        private void TraverseArray(ref MsgReader msgReader)
        {
            if (!msgReader.ReadArray(out int length)) {
                return;
            }
            var data = msgReader.Data;
            for (int n = 0; n < length; n++)
            {
                if (msgReader.Pos >= data.Length) {
                    msgReader.SetEofError();
                    return;
                }
                var type = (MsgFormat)data[msgReader.Pos];
                TraverseElement(type, ref msgReader);   
            }
        }
        
        private void WriteBinElement(ref MsgReader msgReader)
        {
            jsonWriter.ArrayStart(false);
            var bytes = msgReader.ReadBin();
            for (int n = 0; n < bytes.Length; n++) {
                jsonWriter.ElementLng(bytes[n]);
            }
            jsonWriter.ArrayEnd();
        }
        
        private void WriteBinMember(ref MsgReader msgReader)
        {
            jsonWriter.MemberArrayStart(msgReader.KeyName);
            var bytes = msgReader.ReadBin();
            for (int i = 0; i < bytes.Length; i++) {
                jsonWriter.ElementLng(bytes[i]);
            }
            jsonWriter.ArrayEnd();
        }
    }
}
