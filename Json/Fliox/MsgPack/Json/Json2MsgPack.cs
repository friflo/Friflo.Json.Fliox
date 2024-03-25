// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using static Friflo.Json.Burst.JsonEvent;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public sealed class Json2MsgPack
    {
        private MsgWriter       msgWriter = new MsgWriter(new byte[16], true);
        private Utf8JsonParser  parser;
        
        public  bool            HasError        => parser.error.ErrSet;
        public  string          ErrorMessage    => parser.error.GetMessage();
        
        public ReadOnlySpan<byte> ToMsgPack(JsonValue json)
        {
            parser.InitParser(json);
            msgWriter.Init();
            var ev = parser.NextEvent();
            
            WriteElement(ev);
            
            parser.NextEvent(); // read EOF
            if (parser.error.ErrSet) {
                return Array.Empty<byte>();                   
            }
            return msgWriter.Data;
        }
        
        private void WriteElement(JsonEvent ev)
        {
            switch (ev)
            {
                case ValueNull:
                    msgWriter.WriteNull();
                    return;
                case ValueBool:
                    msgWriter.WriteBool(parser.boolValue);
                    return;
                case ValueNumber:
                    WriteElementNumber();
                    return;
                case ValueString:
                    msgWriter.WriteStringUtf8(parser.value.AsSpan());
                    return;
                case ObjectStart:
                    WriteObject();
                    return;
                case ArrayStart:
                    WriteArray();
                    return;
                case Error:
                    return;
                default:
                    return;
            }
        }
        
        private void WriteObject()
        {
            var map = msgWriter.WriteMapFixBegin();
            var count = 0;
            
            while (true)
            {
                var ev  = parser.NextEvent();
                switch (ev)
                {
                    case ValueNull:
                        msgWriter.WriteKeyNil(parser.key.AsSpan(), ref count);
                        continue;
                    case ValueBool:
                        count++;
                        msgWriter.WriteKeyBool(parser.key.AsSpan(), parser.boolValue);
                        continue;
                    case ValueNumber:
                        count++;
                        WriteMemberNumber();
                        continue;
                    case ValueString:
                        msgWriter.WriteKeyStringUtf8(parser.key.AsSpan(), parser.value.AsSpan(), ref count);
                        continue;
                    case ObjectStart:
                        msgWriter.WriteKey(parser.key.AsSpan(), ref count);
                        WriteObject();
                        continue;
                    case ArrayStart:
                        msgWriter.WriteKey(parser.key.AsSpan(), ref count);
                        WriteArray();
                        continue;
                    case ObjectEnd:
                        msgWriter.WriteMapDynEnd(map, count);
                        return;
                    case Error:
                        return;
                    default:
                        return;
                }
            }
        }
        
        private void WriteElementNumber()
        {
            bool success;
            if (parser.isFloat) {
                var dbl = parser.ValueAsDouble(out success);
                if (!success) {
                    return;
                }
                msgWriter.WriteFloat64(dbl);
                return;
            }
            var lng = parser.ValueAsLong(out success);
            if (!success) {
                return;
            }
            msgWriter.WriteInt64(lng);
        }
        
        private void WriteMemberNumber()
        {
            bool success;
            if (parser.isFloat) {
                var dbl = parser.ValueAsDouble(out success);
                if (!success) {
                    return;
                }
                msgWriter.WriteKeyFloat64(parser.key.AsSpan(), dbl);
                return;
            }
            var lng = parser.ValueAsLong(out success);
            if (!success) {
                return;
            }
            msgWriter.WriteKeyInt64(parser.key.AsSpan(), lng);
        }
        
        private void WriteArray()
        {
            int array = msgWriter.WriteArrayFixStart();
            int count = 0;

            while (true)
            {
                var ev  = parser.NextEvent();
                switch (ev) {
                    case ValueNull:
                    case ValueBool:
                    case ValueNumber:
                    case ValueString:
                    case ObjectStart:
                    case ArrayStart:
                        count++;
                        WriteElement(ev);
                        continue;
                    case ArrayEnd:
                        msgWriter.WriteArrayDynEnd(array, count);
                        return;
                    case Error:
                        return;
                    default:
                        throw new NotImplementedException("todo");
                }
            }
        }
    }
}
