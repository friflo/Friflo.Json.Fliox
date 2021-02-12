using System;
using System.IO;

#if JSON_BURST
    using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Friflo.Json.Burst.Utils
{
    public interface IBytesWriter
    {
        void Write(ref ByteList src, int count);
    }
    
    public class StreamBytesWriter: IBytesWriter {
        private readonly Stream stream;
#if JSON_BURST
        private byte[] buffer = new byte[4096];
#endif
        
        public StreamBytesWriter(Stream stream) {
            this.stream = stream;
        }
        
        public unsafe void Write(ref ByteList src, int count) {
#if JSON_BURST
            if (buffer.Length < count)
                buffer = new byte[2 * count];
            
            byte*  srcPtr = &((byte*)src.array.GetUnsafeList()->Ptr) [0];
            fixed (byte* destPtr = &buffer[0])
            {
                UnsafeUtility.MemCpy(destPtr, srcPtr, count);
                stream.Write(buffer, 0, count);
            }
#else
            stream.Write(src.array, 0, count);
#endif
        }
    }
    

}