using System;
using System.IO;

#if JSON_BURST
    using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Friflo.Json.Burst.Utils
{
    public interface IBytesReader
    {
        int Read(ref ByteList dst, int count);
    }
    
    public class StreamBytesReader: IBytesReader {
        private readonly Stream stream;
#if JSON_BURST
        private readonly byte[] buffer = new byte[4096];
#endif
        
        public StreamBytesReader(Stream stream) {
            this.stream = stream;
        }
        
        public unsafe int Read(ref ByteList dst, int count) {
#if JSON_BURST
            int requestBytes = count > 4096 ? 4096 : count;
            int readBytes = stream.Read(buffer, 0, requestBytes);
            byte*  destPtr = &((byte*)dst.array.GetUnsafeList()->Ptr) [0];
            fixed (byte* srcPtr = &buffer[0])
            {
                UnsafeUtility.MemCpy(destPtr, srcPtr, readBytes);
            }
            return readBytes;

#else
            return stream.Read(dst.array, 0, count);
#endif
        }
    }
    
    public class ByteArrayReader: IBytesReader {
        private byte[]      array;
        private int         pos;
        private int         end;
        
        public ByteArrayReader(byte[] array) {
            this.array = array;
            this.pos = 0;
            this.end = array.Length;
        }
        
        public ByteArrayReader(byte[] array, int start, int count) {
            this.array = array;
            this.pos = start;
            this.end = start + count;
        }
        
        public unsafe int Read(ref ByteList dst, int count) {
            int curPos = pos;
            pos += count;
            if (pos > end)
                pos = end;
            
            int len = pos - curPos;
            if (len == 0)
                return 0;
#if JSON_BURST
            byte*  destPtr = &((byte*)dst.array.GetUnsafeList()->Ptr)    [0];
            fixed (byte* srcPtr = &array[curPos])
            {
                UnsafeUtility.MemCpy(destPtr, srcPtr, len);
            }
#else
            Buffer.BlockCopy(array, curPos, dst.array, 0, len);
#endif
            return len;
        }
    }
    
    
}