// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Friflo.Json.Fliox.Utils
{
    public class MemoryBuffer : IDisposable
    {
        private readonly    byte[]          permanent;
        private             byte[]          current;
        private             int             position;
        private readonly    int             capacity;
#if DEBUG
        private readonly    List<byte[]>    buffers = new List<byte[]>();
#endif
        
        public MemoryBuffer(int capacity = 32 * 1024) {
            this.capacity       = capacity;
            permanent           = new byte[capacity];
            current             = permanent;
        }

        public void Dispose() { }
        
        public void Reset() {
            current     = permanent;
            position    = 0;
        }

        [Conditional("DEBUG")]
        public void DebugClear() {
#if DEBUG
            Array.Clear(permanent, 0, permanent.Length);
            foreach (var buffer in buffers) {
                Array.Clear(buffer, 0, buffer.Length);
            }
            buffers.Clear();
#endif
        }
        
        public JsonValue Add(in JsonValue value) {
            int len     = value.Count;
            int start   = position;
            if (start + len <= capacity) {
                // add value to current buffer
                Buffer.BlockCopy(value.Array, value.Start, current, start, len);
                position += len;
                return new JsonValue(current, start, len);
            }
            // create a new buffer and add value
            current     = new byte[capacity];
#if DEBUG
            buffers.Add(current);
#endif
            Buffer.BlockCopy(value.Array, value.Start, current, 0, len);
            position    = len;
            return new JsonValue(current, 0, len);
        }
    }
}