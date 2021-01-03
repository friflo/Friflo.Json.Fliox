// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Burst.Utils
{
#if JSON_BURST
	public struct String32 {
		public Unity.Collections.FixedString32 value;
		
		public String32 (Unity.Collections.FixedString32 src) {
			value = src;
		}
		
		public override String ToString() { return value.ToString(); }
	}
#else
    public struct String32
    {
        public String value;
		
        public String32(String src) {
            value = src;
        }

        public override String ToString() { return value; }
    }
#endif
}