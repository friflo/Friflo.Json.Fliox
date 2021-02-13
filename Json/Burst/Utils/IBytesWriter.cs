using System.IO;

#if JSON_BURST
    using Unity.Burst;
    using System.Collections.Generic;
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
        
        public  void Write(ref ByteList src, int count) {
#if JSON_BURST
            if (buffer.Length < count)
                buffer = new byte[2 * count];
            unsafe {
                byte* srcPtr = &((byte*) src.array.GetUnsafeList()->Ptr)[0];
                fixed (byte* destPtr = &buffer[0]) {
                    UnsafeUtility.MemCpy(destPtr, srcPtr, count);
                }
            }
            stream.Write(buffer, 0, count);
#else
            stream.Write(src.array, 0, count);
#endif
        }
    }
    
#if JSON_BURST
    // Enables IBytesWriter (and by this Stream's) support when compiling with JSON_BURST. 
    static class NonBurstWriter
    {
        private static          int                             writerHandleCounter;
        private static readonly Dictionary<int, IBytesWriter>   JsonWriters = new Dictionary<int, IBytesWriter>();

        public static int AddWriter(IBytesWriter writer) {
            JsonWriters.Add(++writerHandleCounter, writer);
            return writerHandleCounter;
        }

        [BurstDiscard]
        public static void WriteNonBurst(int writerHandle, ref ByteList dst, int count) {
            var writer = JsonWriters[writerHandle];
            writer.Write(ref dst, count);
        }
    }
#endif
    

}