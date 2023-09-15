// Copyright (c) Ullrich Praetz. All rights reserved.
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
        
        public ReadOnlySpan<byte> ToMsgPack(JsonValue json)
        {
            parser.InitParser(json);
            msgWriter.Init();
            var ev = parser.NextEvent();
            
            WriteElement(ev);
            
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
                    if (parser.isFloat) {
                        var value = parser.ValueAsDouble(out bool success);
                        if (!success) {
                            return;
                        }
                        msgWriter.WriteFloat64(value);
                    } else {
                        var value = parser.ValueAsLong(out bool success);
                        if (!success) {
                            return;
                        }
                        msgWriter.WriteInt64(value);
                    }
                    return;
                case ValueString: {
                    var value = parser.value.AsSpan();
                    msgWriter.WriteStringUtf8(value);
                    return;
                }
                case ObjectStart:
                    WriteObject();
                    return;
                case ArrayStart:
                    WriteArray();
                    return;
            }
        }
        
        private void WriteObject()
        {
            var map = msgWriter.WriteMap32Begin();
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
                        if (parser.isFloat) {
                            var value = parser.ValueAsDouble(out bool success);
                            if (!success) {
                                return;
                            }
                            msgWriter.WriteKeyFloat64(parser.key.AsSpan(), value);
                        } else {
                            var value = parser.ValueAsLong(out bool success);
                            if (!success) {
                                return;
                            }
                            msgWriter.WriteKeyInt64(parser.key.AsSpan(), value);
                        }
                        continue;
                    case ValueString: {
                        var value = parser.value.AsSpan();
                        msgWriter.WriteKeyStringUtf8(parser.key.AsSpan(), value, ref count);
                        continue;
                    }
                    case ObjectStart:
                        msgWriter.WriteKey(parser.key.AsSpan(), ref count);
                        WriteObject();
                        continue;
                    case ArrayStart:
                        msgWriter.WriteKey(parser.key.AsSpan(), ref count);
                        WriteArray();
                        continue;
                    case ObjectEnd:
                        msgWriter.WriteMap32End(map, count);
                        return;
                }
            }
        }
        
        private void WriteArray()
        {
            int array = msgWriter.WriteArray32Start();
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
                        msgWriter.WriteArray32End(array, count);
                        return;
                    default:
                        throw new NotImplementedException("todo");
                }
            }
        }
    }
}
