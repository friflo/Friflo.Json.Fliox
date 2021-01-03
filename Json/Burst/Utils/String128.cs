// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Burst.Utils
{
#if JSON_BURST
	public struct String128 {
		public Unity.Collections.FixedString128 value;
		
		public String128 (Unity.Collections.FixedString128 src) {
			value = src;
		}
		
		public override String ToString() { return value.ToString(); }
	}
#else
	public struct String128 {
		public String value;
		
		public String128 (String src) {
			value = src;
		}

		public override String ToString() { return value; }
	}
#endif
	
}
