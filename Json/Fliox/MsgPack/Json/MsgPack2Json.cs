// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;
using static Friflo.Json.Fliox.MsgPack.MsgFormat;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public sealed class MsgPack2Json
    {
        // --- private fields
                        private     Utf8JsonWriter      jsonWriter;
        [Browse(Never)] private     string              error;
                        private     MsgReaderState      readerState;
                        private     StringBuilder       errorBuilder; 
        
        // --- public properties
                        public      string              Error       => error;
                        public      MsgReaderState      ReaderState => readerState;

        public override string ToString() => readerState == MsgReaderState.Ok ? "Ok" : Error;
        
        public JsonValue ToJson(ReadOnlySpan<byte> msg)
        {
            jsonWriter.InitSerializer();
            var msgReader = new MsgReader(msg);
            
            Start(ref msgReader);
            
            readerState = msgReader.State;
            if (msgReader.State == MsgReaderState.Ok) {
                error = null;
                return new JsonValue(jsonWriter.json);
            }
            errorBuilder ??= new StringBuilder();
            errorBuilder.Clear();
            error = msgReader.CreateErrorMessage(errorBuilder);
            errorBuilder.Clear();
            return default;
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
                case    nil:
                    jsonWriter.ElementNul();
                    return;
                case    True:
                    jsonWriter.ElementBln(true);
                    return;
                case    False:
                    jsonWriter.ElementBln(false);
                    return;
                case    bin8:
                case    bin16:
                case    bin32:
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
                case    int64:
                    jsonWriter.ElementLng(msgReader.ReadInt64());
                    return;
                case    float32:
                    jsonWriter.ElementDbl(msgReader.ReadFloat32());
                    return;
                case    float64:
                    jsonWriter.ElementDbl(msgReader.ReadFloat64());
                    return;
                case >= fixstr and <= fixstrMax:
                case    str8:
                case    str16:
                case    str32:
                    jsonWriter.ElementStr(msgReader.ReadStringSpan());
                    return;
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
                    case    nil:
                        jsonWriter.MemberNul(msgReader.KeyName);
                        continue;
                    case    True:
                        jsonWriter.MemberBln(msgReader.KeyName, true);
                        continue;
                    case    False:
                        jsonWriter.MemberBln(msgReader.KeyName, false);
                        continue;
                    case    bin8:
                    case    bin16:
                    case    bin32:
                        WriteBinMember(ref msgReader);
                        continue;
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
                    case    int64:
                        jsonWriter.MemberLng(msgReader.KeyName, msgReader.ReadInt64());
                        continue;
                    case    float32:
                        jsonWriter.MemberDbl(msgReader.KeyName, msgReader.ReadFloat32());
                        continue;
                    case    float64:
                        jsonWriter.MemberDbl(msgReader.KeyName, msgReader.ReadFloat64());
                        continue;
                    case >= fixstr and <= fixstrMax:
                    case    str8:
                    case    str16:
                    case    str32:
                        jsonWriter.MemberStr(msgReader.KeyName,  msgReader.ReadStringSpan());
                        continue;
                    case >= fixmap and <= fixmapMax:
                    case    map16:
                    case    map32:
                        jsonWriter.MemberObjectStart(msgReader.KeyName);
                        TraverseObject(ref msgReader);
                        jsonWriter.ObjectEnd();
                        continue;
                    case >= fixarray and <= fixarrayMax:
                    case    array16:
                    case    array32:
                        jsonWriter.MemberArrayStart(msgReader.KeyName);
                        TraverseArray(ref msgReader);
                        jsonWriter.ArrayEnd();
                        continue;
                    default:
                        msgReader.SkipTree();
                        continue;
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
