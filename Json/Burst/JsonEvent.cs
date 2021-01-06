// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
namespace Friflo.Json.Burst
{
    /// <summary>
    /// The set of all JSON events returned by <see cref="JsonParser.NextEvent()"/> while iterating a JSON document.
    /// </summary>
    public enum JsonEvent
    {
        /// <summary>
        /// Found an object member { "name": "John" } with a string value in case the previous event was <see cref="ObjectStart"/>
        /// or an array element ["John"] in case the previous event was <see cref="ArrayStart"/>.<br/>
        /// The value is available via <see cref="JsonParser.value"/>.
        /// To get its common .NET representation as a <see cref="string"/> use <see cref="Bytes.ToString()"/><br/>  
        /// In case of an object member <see cref="JsonParser.key"/> is set.  
        /// </summary>
        ValueString,
        /// <summary>
        /// Found an object member { "count": 11 } with a number value in case the previous event was <see cref="ObjectStart"/>
        /// or an array element [11] in case the previous event was <see cref="ArrayStart"/>.<br/>
        /// In case of an object member <see cref="JsonParser.key"/> is set.<br/>
        ///
        /// The value is available via <see cref="JsonParser.value"/>.<br/>
        /// If the number is floating point number <see cref="JsonParser.isFloat"/> is set. Otherwise the value is an integer.<br/>
        /// To get the value as <see cref="double"/> or <see cref="float"/> use <see cref="JsonParser.ValueAsDouble"/> or <see cref="JsonParser.ValueAsFloat"/><br/>
        /// To get the value as long or int use <see cref="JsonParser.ValueAsLong"/> or <see cref="JsonParser.ValueAsInt"/>
        /// </summary>
        ValueNumber,
        /// <summary>
        /// Found an object member { "isAlive": true } with a boolean value in case the previous event was <see cref="ObjectStart"/>
        /// or an array element [true] in case the previous event was <see cref="ArrayStart"/>.<br/>
        /// The value is available via <see cref="JsonParser.boolValue"/>.<br/>
        /// In case of an object member <see cref="JsonParser.key"/> is set.
        /// </summary>
        ValueBool,
        /// <summary>
        /// Found an object member { "spouse": null } with a null value in case the previous event was <see cref="ObjectStart"/>
        /// or an array element [null] in case the previous event was <see cref="ArrayStart"/>.<br/>
        /// Additional data is not available as the only value is null.<br/>
        /// In case of an object member <see cref="JsonParser.key"/> is set.
        /// </summary>
        ValueNull,
        /// <summary>
        /// Found an object member "employee": { ... } with an object in case the previous event was <see cref="ObjectStart"/>
        /// or an array element [{ ... }] in case the previous event was <see cref="ArrayStart"/>.<br/>
        /// Additional data is not available for this event. To access embedded object members use <see cref="JsonParser.NextEvent()"/><br/>
        /// In case of an object member <see cref="JsonParser.key"/> is set.
        /// </summary>
        ObjectStart,
        /// <summary>
        /// Found the end of an JSON object previously started with <see cref="ObjectStart"/><br/>
        /// Additional data is not available for this event. To access embedded object members use <see cref="JsonParser.NextEvent()"/><br/>
        /// </summary>
        ObjectEnd,
        /// <summary>
        /// Found an object member "employee": [ ... ] with an array in case the previous event was <see cref="ObjectStart"/>
        /// or an array element [ [...] ] in case the previous event was <see cref="ArrayStart"/>.<br/>
        /// Additional data is not available for this event. To access embedded array elements use <see cref="JsonParser.NextEvent()"/><br/>
        /// In case of an object member <see cref="JsonParser.key"/> is set.
        /// </summary>
        ArrayStart,
        /// Found the end of an JSON array previously started with <see cref="ArrayStart"/><br/>
        /// Additional data is not available for this event. To access embedded object members use <see cref="JsonParser.NextEvent()"/><br/>
        ArrayEnd,
        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// After iteration of a valid JSON document <see cref="JsonParser.NextEvent()"/> returns <see cref="EOF"/>
        /// when reaching the end of the given payload.
        /// </summary>
        EOF,
        /// <summary>
        /// Notify a JSON error while parsing.<br/>
        /// Calling <see cref="JsonParser.NextEvent()"/> after <see cref="EOF"/> returned once it always returns <see cref="Error"/>
        /// </summary>
        Error
    }

    public struct JsonEventUtils
    {
        public static void AppendEvent(JsonEvent ev, ref Bytes bytes) {
            switch (ev) {
                case JsonEvent.ValueString: bytes.AppendStr32("ValueString");   break; 
                case JsonEvent.ValueNumber: bytes.AppendStr32("ValueNumber");   break; 
                case JsonEvent.ValueBool:   bytes.AppendStr32("ValueBool");     break; 
                case JsonEvent.ValueNull:   bytes.AppendStr32("ValueNull");     break; 
                case JsonEvent.ObjectStart: bytes.AppendStr32("ObjectStart");   break; 
                case JsonEvent.ObjectEnd:   bytes.AppendStr32("ObjectEnd");     break; 
                case JsonEvent.ArrayStart:  bytes.AppendStr32("ArrayStart");    break; 
                case JsonEvent.ArrayEnd:    bytes.AppendStr32("ArrayEnd");      break; 
                case JsonEvent.EOF:         bytes.AppendStr32("EOF");           break; 
                case JsonEvent.Error:       bytes.AppendStr32("Error");         break; 
            }
        }
    }
}