// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Mapper.Map.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public static class ReadUtils
    {
    
        public static readonly int minLen = 0;

        public static int Inc(int len) {
            return len == 0 ? 1 : 2 * len;
        }
        
        public static TVal ErrorIncompatible<TVal>(JsonReader reader, string msg, TypeMapper expectType, out bool success) {
            ErrorIncompatible<TVal>(reader, msg, "", expectType, out success);
            success = false;
            return default;
        }

        public static TVal ErrorIncompatible<TVal>(JsonReader reader, string msg, string msgParam, TypeMapper expectType, out bool success) {
            ref Bytes strBuf = ref reader.strBuf;
            ref var parser = ref reader.parser;

#pragma warning disable 162
            // ReSharper disable HeuristicUnreachableCode
            if (false) { // similar error message by string interpolation for illustration
                var _ = $"Cannot assign {parser.Event} to {msg}. Expect: {expectType.type}, got: {parser.value}";
            }
#pragma warning restore 162

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
            strBuf.AppendString(expectType.type.ToString());
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