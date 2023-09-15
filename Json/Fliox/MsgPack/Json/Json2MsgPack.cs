// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

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
                case JsonEvent.ValueNull:
                    msgWriter.WriteNull();
                    return;
                case JsonEvent.ValueBool:
                    msgWriter.WriteBool(parser.boolValue);
                    return;
                case JsonEvent.ValueNumber:
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
                case JsonEvent.ValueString: {
                    var value = parser.value.AsSpan();
                    msgWriter.WriteStringUtf8(value);
                    return;
                }
                case JsonEvent.ObjectStart: {
                    WriteObject();
                    return;
                }
                case JsonEvent.ArrayStart: {
                    WriteArray();
                    return;
                }
            }
        }
        
        private void WriteObject()
        {
            var map = msgWriter.WriteMap32Begin();
            var count = 0;
            
            while (true)
            {
                var ev  = parser.NextEvent();
                var key = parser.key.AsSpan();
                switch (ev) {
                    case JsonEvent.ValueNull:
                        msgWriter.WriteKeyNil(key, ref count);
                        continue;
                    case JsonEvent.ValueBool:
                        count++;
                        msgWriter.WriteKeyBool(key, parser.boolValue);
                        continue;
                    case JsonEvent.ValueNumber:
                        count++;
                        if (parser.isFloat) {
                            var value = parser.ValueAsDouble(out bool success);
                            if (!success) {
                                return;
                            }
                            msgWriter.WriteKeyFloat64(key, value);
                        } else {
                            var value = parser.ValueAsLong(out bool success);
                            if (!success) {
                                return;
                            }
                            msgWriter.WriteKeyInt64(key, value);
                        }
                        continue;
                    case JsonEvent.ValueString: {
                        var value = parser.value.AsSpan();
                        msgWriter.WriteKeyStringUtf8(key, value, ref count);
                        continue;
                    }
                    case JsonEvent.ObjectStart: {
                        msgWriter.WriteKey(key, ref count);
                        WriteObject();
                        continue;
                    }
                    case JsonEvent.ArrayStart: {
                        msgWriter.WriteKey(key, ref count);
                        WriteArray();
                        continue;
                    }
                    case JsonEvent.ObjectEnd:
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
                while (true)
                {
                    var ev  = parser.NextEvent();
                    switch (ev) {
                        case JsonEvent.ValueNull:
                        case JsonEvent.ValueBool:
                        case JsonEvent.ValueNumber:
                        case JsonEvent.ValueString:
                        case JsonEvent.ObjectStart:
                        case JsonEvent.ArrayStart:
                            count++;
                            WriteElement(ev);
                            continue;
                        case JsonEvent.ArrayEnd:
                            msgWriter.WriteArray32End(array, count);
                            return;
                        default:
                            throw new NotImplementedException("todo");
                    }
                }
            }
        }
    }
}
