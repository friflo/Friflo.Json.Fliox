// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
namespace Friflo.Json.Burst
{
    public enum JsonEvent
    {
        ValueString,	// key is set, if inside an object
        ValueNumber,	// key is set, if inside an object
        ValueBool,		// true, false. key is set, if inside an object
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