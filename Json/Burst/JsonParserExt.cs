// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
#if JSON_BURST
	using Str32 = Unity.Collections.FixedString32;
	using Str128 = Unity.Collections.FixedString128;
#else
	using Str32 = System.String;
	using Str128 = System.String;
#endif

namespace Friflo.Json.Burst
{
    public partial struct JsonParser
    {
	    public bool IsObj(JsonEvent ev, ref Str32 name) {
		    return ev == JsonEvent.ObjectStart && key.IsEqual32(name);
	    }
	    
	    public bool IsObj(JsonEvent ev, Str32 name) {
		    return ev == JsonEvent.ObjectStart && key.IsEqual32(name);
	    }
		// ---
		public bool IsArr(JsonEvent ev, ref Str32 name) {
			return ev == JsonEvent.ArrayStart && key.IsEqual32(name);
		}
		
	    public bool IsArr(JsonEvent ev, Str32 name) {
		    return ev == JsonEvent.ArrayStart && key.IsEqual32(name);
	    }
		
	    // ---
	    public bool IsNum(JsonEvent ev, ref Str32 name) {
		    return ev == JsonEvent.ValueNumber && key.IsEqual32(name);
	    }
	    
	    public bool IsNum(JsonEvent ev, Str32 name) {
		    return ev == JsonEvent.ValueNumber && key.IsEqual32(name);
	    }
		
	    // ---
	    public bool IsStr(JsonEvent ev, ref Str32 name) {
		    return ev == JsonEvent.ValueString && key.IsEqual32(name);
	    }
	    
	    public bool IsStr(JsonEvent ev, Str32 name) {
		    return ev == JsonEvent.ValueString && key.IsEqual32(name);
	    }
	    
	    // ---
	    public bool IsBln(JsonEvent ev, ref Str32 name) {
		    return ev == JsonEvent.ValueBool && key.IsEqual32(name);
	    }
	    
	    public bool IsBln(JsonEvent ev, Str32 name) {
		    return ev == JsonEvent.ValueBool && key.IsEqual32(name);
	    }
	    
	    // ---
	    public bool IsNul(JsonEvent ev, ref Str32 name) {
		    return ev == JsonEvent.ValueNull && key.IsEqual32(name);
	    }
	    
	    public bool IsNul(JsonEvent ev, Str32 name) {
		    return ev == JsonEvent.ValueNull && key.IsEqual32(name);
	    }
    }
}