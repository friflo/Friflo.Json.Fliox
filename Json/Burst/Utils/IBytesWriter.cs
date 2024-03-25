// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.IO;

namespace Friflo.Json.Burst.Utils
{
    // JSON_BURST_TAG
    public interface IBytesWriter
    {
        void Write(byte[] src, int count);
    }
    
    public sealed class StreamBytesWriter: IBytesWriter {
        private readonly Stream stream;
        
        public StreamBytesWriter(Stream stream) {
            this.stream = stream;
        }
        
        public  void Write(byte[] src, int count) {
            stream.Write(src, 0, count);
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