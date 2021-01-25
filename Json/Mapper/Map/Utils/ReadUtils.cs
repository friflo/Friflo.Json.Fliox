// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public static class ReadUtils
    {
    
        public static readonly int minLen = 8;

        public static int Inc(int len) {
            return len < 5 ? minLen : 2 * len;
        }
        
        public static TVal ErrorIncompatible<TVal>(JsonReader reader, string msg, ITypeMapper expectType, ref JsonParser parser, out bool success) {
            ErrorIncompatible<TVal>(reader, msg, "", expectType, ref parser, out success);
            success = false;
            return default;
        }

        public static TVal ErrorIncompatible<TVal>(JsonReader reader, string msg, string msgParam, ITypeMapper expectType, ref JsonParser parser, out bool success) {
            ref Bytes strBuf = ref reader.strBuf;
            /*
            string evType = null;
            string value = null;
            switch (parser.Event) {
                case JsonEvent.ValueBool:   evType = "bool";   value = parser.boolValue ? "true" : "false"; break;
                case JsonEvent.ValueString: evType = "string"; value = "'" + parser.value + "'";            break;
                case JsonEvent.ValueNumber: evType = "number"; value = parser.value.ToString();             break;
                case JsonEvent.ValueNull:   evType = "null";   value = "null";                              break;
                case JsonEvent.ArrayStart:  evType = "array";  value = "[...]";                             break;
                case JsonEvent.ObjectStart: evType = "object"; value = "{...}";                             break;
            } */
            // Error format: $"Cannot assign {evType} to {msg}. Expect: {expectType.type.Name}, got: {value}";
            strBuf.Clear();
            strBuf.AppendString("Cannot assign ");
            switch (parser.Event) {
                case JsonEvent.ValueBool:   strBuf.AppendString("bool");    break;
                case JsonEvent.ValueString: strBuf.AppendString("string");  break;
                case JsonEvent.ValueNumber: strBuf.AppendString("number");  break;
                case JsonEvent.ValueNull:   strBuf.AppendString("null");    break;
                case JsonEvent.ArrayStart:  strBuf.AppendString("array");   break;
                case JsonEvent.ObjectStart: strBuf.AppendString("object");  break;
            }
            strBuf.AppendString(" to ");
            strBuf.AppendString(msg);
            strBuf.AppendString(msgParam);
            strBuf.AppendString(". Expect: ");
            strBuf.AppendString(expectType.GetNativeType().ToString());
            strBuf.AppendString(", got: ");
            switch (parser.Event) {
                case JsonEvent.ValueBool:   strBuf.AppendString(parser.boolValue ? "true" : "false");       break;
                case JsonEvent.ValueString:
                    strBuf.AppendChar('\'');strBuf.AppendBytes(ref parser.value); strBuf.AppendChar('\'');  break;
                case JsonEvent.ValueNumber: strBuf.AppendBytes(ref parser.value);                           break;
                case JsonEvent.ValueNull:   strBuf.AppendString("null");                                    break;
                case JsonEvent.ArrayStart:  strBuf.AppendString("[...]");                                   break;
                case JsonEvent.ObjectStart: strBuf.AppendString("{...}");                                   break;
            }
            parser.ErrorMsg("JsonReader", ref strBuf);
            success = false;
            return default;
        }
        
        public static TVal ErrorMsg<TVal>(JsonReader reader, string msg, string value, out bool success) {
            ref Bytes strBuf = ref reader.strBuf;
            strBuf.Clear();
            strBuf.AppendString(msg);
            strBuf.AppendString(value);
            reader.parser.ErrorMsg("JsonReader", ref strBuf);
            success = false;
            return default;
        }

        public static TVal ErrorMsg<TVal>(JsonReader reader, string msg, JsonEvent ev, out bool success) {
            reader.strBuf.Clear();
            reader.strBuf.AppendString(msg);
            JsonEventUtils.AppendEvent(ev, ref reader.strBuf);
            reader.parser.ErrorMsg("JsonReader", ref reader.strBuf);
            success = false;
            return default;
        }

        public static TVal ErrorMsg<TVal>(JsonReader reader, string msg, ref Bytes value, out bool success) {
            reader.parser.ErrorMsgParam("JsonReader", msg, ref value);
            success = false;
            return default;
        }
        
        /** Method only exist to find places, where token (numbers) are parsed. E.g. in or double */
        public static bool ValueParseError(JsonReader reader) {
            return false; // ErrorNull(parser.parseCx.GetError().ToString());
        }

    }
}