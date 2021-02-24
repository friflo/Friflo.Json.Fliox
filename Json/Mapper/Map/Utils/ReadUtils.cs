// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Burst;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Mapper.Map
{

    public partial struct Reader
    {
    
        public static readonly int minLen = 0;

        public static int Inc(int len) {
            return len == 0 ? 1 : 2 * len;
        }
        
        public TVal ErrorIncompatible<TVal>(string msg, TypeMapper expectType, out bool success) {
            ErrorIncompatible<TVal>(msg, "", expectType, out success);
            success = false;
            return default;
        }

        public TVal ErrorIncompatible<TVal>(string msg, string msgParam, TypeMapper expectType, out bool success) {

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
        
        public TVal ErrorMsg<TVal>(string msg, string value, out bool success) {
            strBuf.Clear();
            strBuf.AppendString(msg);
            strBuf.AppendString(value);
            parser.ErrorMsg("JsonReader", ref strBuf);
            success = false;
            return default;
        }

        public TVal ErrorMsg<TVal>(string msg, JsonEvent ev, out bool success) {
            strBuf.Clear();
            strBuf.AppendString(msg);
            JsonEventUtils.AppendEvent(ev, ref strBuf);
            parser.ErrorMsg("JsonReader", ref strBuf);
            success = false;
            return default;
        }

        public TVal ErrorMsg<TVal>(string msg, ref Bytes value, out bool success) {
            parser.ErrorMsgParam("JsonReader", msg, ref value);
            success = false;
            return default;
        }
        
        /** Method only exist to find places, where token (numbers) are parsed. E.g. in or double */
        public bool ValueParseError() {
            return false; // ErrorNull(parser.parseCx.GetError().ToString());
        }

    }
}