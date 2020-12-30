using System;

namespace Friflo.Json.Burst.Utils
{
#if JSON_BURST
	public struct String32 {
		public Unity.Collections.FixedString32 value;
		
		public String32 (Unity.Collections.FixedString32 src) {
			value = src;
		}
		
		public String32 (ref Bytes bytes) {
			value = new Unity.Collections.FixedString32();
			ref var buf = ref bytes.buffer.array;
			for (int i = bytes.start; i < bytes.end; i++)
				value.Add(buf[i]);
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