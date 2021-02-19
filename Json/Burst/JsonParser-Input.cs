// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Text;
using Friflo.Json.Burst.Utils;

#if JSON_BURST
    using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Friflo.Json.Burst
{
    enum InputType {
        ByteList,
        ByteReader,
    }
    
    public partial struct JsonParser
    {
        public string DebugString { get {
                int start = pos - 20;
                if (start < 0)
                    start = 0;
                int end = pos + 30;
                if (end > bufEnd)
                    end = bufEnd;
                var builder = new StringBuilder();
                int n = start;
                for (; n < end; n++) {
                    if (n == pos)
                        builder.Append(" =>");
                    builder.Append((char) buf.buffer.array[n]);
                }
                if (pos == bufEnd)
                    builder.Append(" =>");
                
                if (end == bufEnd)
                    builder.Append("<buffer end>");
                
                return $"pos: ({pos})  {builder}";
            }
        }
        
        public void InitParser(Bytes bytes) {
            InitParser (bytes.buffer, bytes.start, bytes.Len);
        }

        private bool Read() {
            if (pos != bufEnd)
                throw new InvalidOperationException("expect pos != bufEnd in Read() pos: " + pos);

            if (!inputStreamOpen)
                return false;

            int readBytes;
            
            switch (inputType) {
                case InputType.ByteList:
                    readBytes = ReadByteList();
                    break;
                case InputType.ByteReader:
#if JSON_BURST
                    readBytes = 0;
                    NonBurstReader.ReadNonBurst(readerHandle, ref buf.buffer, ref readBytes, BufSize);
#else
                    readBytes = bytesReader.Read(ref buf.buffer, BufSize);
#endif
                    break;
                default:
                    throw new NotImplementedException("inputType: " + inputType);
            }

            if (readBytes != 0) {
                bufEnd = readBytes;
                bufferCount += pos;
                pos = 0;
                return true;
            }
            inputStreamOpen = false;
            return false;
        }
        
        public void InitParser(StreamBytesReader reader) {
            inputType       = InputType.ByteReader;
#if JSON_BURST
            readerHandle    = NonBurstReader.AddReader(reader);
#else
            bytesReader     = reader;
#endif
            Start();
        }
        
        public void InitParser(Stream stream) {
            inputType           = InputType.ByteReader;
            IBytesReader reader  = new StreamBytesReader(stream);
#if JSON_BURST
            readerHandle = NonBurstReader.AddReader(reader);
#else
            bytesReader = reader;
#endif
            Start();
        }
        
        public void InitParser(byte[] array, int start, int count) {
            inputType           = InputType.ByteReader;
            IBytesReader reader  = new ByteArrayReader(array, start, count);
#if JSON_BURST
            readerHandle = NonBurstReader.AddReader(reader);
#else
            bytesReader = reader;
#endif
            Start();
        }

        public void InitParser(ByteList bytes, int start, int len) {
            inputType       = InputType.ByteList;
            inputByteList   = bytes;
            inputArrayPos   = start;
            inputArrayEnd   = start + len;
            Start();
        }
        
        // ReSharper disable once RedundantUnsafeContext
        private unsafe int ReadByteList() {
            int curPos = inputArrayPos;
            inputArrayPos += BufSize;
            if (inputArrayPos > inputArrayEnd)
                inputArrayPos = inputArrayEnd;
            
            int len = inputArrayPos - curPos;
            if (len == 0)
                return 0;
#if JSON_BURST
            byte*  srcPtr =  &((byte*)inputByteList.array.GetUnsafeList()->Ptr) [curPos];
            byte*  destPtr = &((byte*)buf.buffer.array.GetUnsafeList()->Ptr)    [0];
            UnsafeUtility.MemCpy(destPtr, srcPtr, len);
#else
            Buffer.BlockCopy(inputByteList.array, curPos, buf.buffer.array, 0, len);
#endif
            return len;
        }
    }
}


