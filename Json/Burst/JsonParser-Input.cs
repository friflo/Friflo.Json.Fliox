// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Text;
using Friflo.Json.Burst.Utils;

namespace Friflo.Json.Burst
{
    enum InputType {
        ByteArray,
        ByteReader,
    }
    
    public partial struct Utf8JsonParser
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
                    builder.Append((char) buf.buffer[n]);
                }
                if (pos == bufEnd)
                    builder.Append(" =>");
                
                if (end == bufEnd)
                    builder.Append("<buffer end>");
                
                return $"pos: ({pos})  {builder}";
            }
        }
        
        public void InitParser(Bytes bytes) {
            int len = bytes.end - bytes.start;
            InitParser (bytes.buffer, bytes.start, len);
        }

        private bool Read() {
            if (pos != bufEnd)
                throw new InvalidOperationException("expect pos != bufEnd in Read() pos: " + pos);

            if (!inputStreamOpen)
                return false;

            int readBytes;
            
            switch (inputType) {
                case InputType.ByteArray:
                    readBytes = ReadByteArray();
                    break;
                case InputType.ByteReader:
#if JSON_BURST
                    readBytes = 0;
                    NonBurstReader.ReadNonBurst(readerHandle, ref buf.buffer, ref readBytes, BufSize);
#else
                    readBytes = bytesReader.Read(buf.buffer, BufSize);
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
        
/*      public void InitParser(StreamBytesReader reader) {
            inputType       = InputType.ByteReader;
#if JSON_BURST
            readerHandle    = NonBurstReader.AddReader(reader);
#else
            bytesReader     = reader;
#endif
            Start();
        }
*/

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
        
        /*
        public void InitParser(byte[] array, int start, int count) {
            inputType           = InputType.ByteReader;
            IBytesReader reader  = new ByteArrayReader(array, start, count);
#if JSON_BURST
            readerHandle = NonBurstReader.AddReader(reader);
#else
            bytesReader = reader;
#endif
            Start();
        } */

        public void InitParser(byte[] bytes, int start, int len) {
            inputType       = InputType.ByteArray;
            inputArray      = bytes;
            inputArrayPos   = start;
            inputArrayEnd   = start + len;
            Start();
        }
        
        // ReSharper disable once RedundantUnsafeContext
        private unsafe int ReadByteArray() {
            int curPos = inputArrayPos;
            inputArrayPos += BufSize;
            if (inputArrayPos > inputArrayEnd)
                inputArrayPos = inputArrayEnd;
            
            int len = inputArrayPos - curPos;
            if (len == 0) {
                return 0;
            }
            Buffer.BlockCopy(inputArray, curPos, buf.buffer, 0, len);
            return len;
        }
    }
}


