// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.IO;
using System;

// ReSharper disable RedundantUnsafeContext
namespace Friflo.Json.Burst.Utils
{
    // JSON_BURST_TAG
    public interface IBytesReader
    {
        int Read(byte[] dst, int count);
    }
    
    public sealed class StreamBytesReader: IBytesReader {
        private readonly Stream stream;
        
        public StreamBytesReader(Stream stream) {
            this.stream = stream;
        }
        
        public unsafe int Read(byte[] dst, int count) {
            return stream.Read(dst, 0, count);
        }
    }
    
    public sealed class ByteArrayReader: IBytesReader {
        private readonly    byte[]  array;
        private             int     pos;
        private readonly    int     end;
        
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
        
        public int Read(byte[] dst, int count) {
            int curPos = pos;
            pos += count;
            if (pos > end)
                pos = end;
            
            int len = pos - curPos;
            if (len == 0)
                return 0;
            Buffer.BlockCopy(array, curPos, dst, 0, len);
            return len;
        }
    }
    
#if JSON_BURST
    static class NonBurstReader
    {
        private static          int                             readerHandleCounter;
        private static readonly Dictionary<int, IBytesReader>   JsonReaders = new Dictionary<int, IBytesReader>();

        public static int AddReader(IBytesReader reader) {
            JsonReaders.Add(++readerHandleCounter, reader);
            return readerHandleCounter;
        }

        [BurstDiscard]
        public static void ReadNonBurst(int readerHandle, ref ByteList dst, ref int readBytes, int count) {
            var reader = JsonReaders[readerHandle];
            readBytes = reader.Read(ref dst, count);
        }
    }
#endif
}