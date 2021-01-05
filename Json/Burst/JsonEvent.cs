// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
namespace Friflo.Json.Burst
{
    public enum JsonEvent
    {
        /// <summary>
        /// Found an object member { "name": "John" } with a string value in case the previous event was <see cref="ObjectStart"/>
        /// or an array element ["John"] in case the previous event was <see cref="ArrayStart"/>.
        /// The value is available via <see cref="JsonParser.value"/>.
        /// In case of an object member <see cref="JsonParser.key"/> is set.  
        /// </summary>
        ValueString,
        /// <summary>
        /// Found an object member { "count": 11 } with a number value in case the previous event was <see cref="ObjectStart"/>
        /// or an array element [11] in case the previous event was <see cref="ArrayStart"/>.
        /// In case of an object member <see cref="JsonParser.key"/> is set.  
        ///
        /// The value is available via <see cref="JsonParser.value"/>.
        /// If the number is floating point number <see cref="JsonParser.isFloat"/> is set. Other the value is an integer.
        /// To get the value as double or float use <see cref="JsonParser.ValueAsDouble"/> or <see cref="JsonParser.ValueAsFloat"/>
        /// To get the value as long or int use <see cref="JsonParser.ValueAsLong"/> or <see cref="JsonParser.ValueAsInt"/>
        /// </summary>
        ValueNumber,
        /// <summary>
        /// Found an object member { "isAlive": true } with a boolean value in case the previous event was <see cref="ObjectStart"/>
        /// or an array element [true] in case the previous event was <see cref="ArrayStart"/>.
        /// The value is available via <see cref="JsonParser.boolValue"/>.
        /// In case of an object member <see cref="JsonParser.key"/> is set.
        /// </summary>
        ValueBool,		// true, false. key is set, if inside an object
        /// <summary>
        /// Found an object member { "spouse": null } with a null value in case the previous event was <see cref="ObjectStart"/>
        /// or an array element [null] in case the previous event was <see cref="ArrayStart"/>.
        /// Additional data is not available as the only value is null.
        /// In case of an object member <see cref="JsonParser.key"/> is set.
        /// </summary>
        ValueNull,		// key is set, if inside an object
	
        ObjectStart,	// key is set, if inside an object
        ObjectEnd,
	
        ArrayStart,		// key is set, if inside an object
        ArrayEnd,
	
        // ReSharper disable once InconsistentNaming
        EOF,            // After iteration of a valid JSON tree NextEvent() returns JsonEvent.EOF when reaching the end of the given payload. 
        Error,          // Notify JSON error while parsing. Calling NextEvent() after JsonEvent.EOF return JsonEvent.Error
    }

    public struct JsonEventUtils
    {
        public static void AppendEvent(JsonEvent ev, ref Bytes bytes) {
            switch (ev) {
                case JsonEvent.ValueString:	bytes.AppendStr32("ValueString");	break; 
                case JsonEvent.ValueNumber: bytes.AppendStr32("ValueNumber");	break; 
                case JsonEvent.ValueBool:	bytes.AppendStr32("ValueBool");	    break; 
                case JsonEvent.ValueNull:	bytes.AppendStr32("ValueNull");	    break; 
                case JsonEvent.ObjectStart: bytes.AppendStr32("ObjectStart");	break; 
                case JsonEvent.ObjectEnd:	bytes.AppendStr32("ObjectEnd");	    break; 
                case JsonEvent.ArrayStart:	bytes.AppendStr32("ArrayStart");	break; 
                case JsonEvent.ArrayEnd:	bytes.AppendStr32("ArrayEnd");		break; 
                case JsonEvent.EOF:			bytes.AppendStr32("EOF");			break; 
                case JsonEvent.Error:		bytes.AppendStr32("Error");		    break; 
            }
        }
    }
}