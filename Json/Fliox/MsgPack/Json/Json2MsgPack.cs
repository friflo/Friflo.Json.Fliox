// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.MsgPack.Json
{
    public class Json2MsgPack
    {
        private MsgWriter       msgWriter;
        private Utf8JsonParser  parser;
        
        public ReadOnlySpan<byte> ToMsgPack(JsonValue json)
        {
            parser.InitParser(json);
            WriteElement();
            return msgWriter.Data;
        }
        
        private void WriteElement()
        {
            while (true)
            {
                var ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNull:
                        msgWriter.WriteNull();
                        break;
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
                        break;
                    case JsonEvent.ValueString: {
                        var value = parser.value.AsSpan();
                        msgWriter.WriteStringUtf8(value);
                        break;
                    }
                    case JsonEvent.ObjectStart: {
                        WriteObject();
                        break;
                    }
                    case JsonEvent.ArrayStart: {
                        WriteArray();
                        break;
                    }
                }
            }
        }
        
        private void WriteObject()
        {
            var map = msgWriter.WriteMap32Begin();
            var count = 1;
            
            msgWriter.WriteMap32End(map, count);
        }
        
        private void WriteArray()
        {
            int array = msgWriter.WriteArray32Start();
            int count = 0;
            msgWriter.WriteArray32End(array, count);
        }
    }
}
