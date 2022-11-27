// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Utils
{
    public class MemoryBuffer : IDisposable
    {
        private readonly    byte[]          permanent;
        private             byte[]          current;
        private             int             position;
        private readonly    int             capacity;
        
        public MemoryBuffer(bool reuse, int capacity = 32 * 1024) {
            this.capacity       = capacity;
            permanent           = reuse ? new byte[capacity] : null;
            position            = reuse ? 0: capacity;
        }
        
        public void Dispose() { }
        
        public void Reset() {
            current     = permanent;
            position    = 0;
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
            Buffer.BlockCopy(value.Array, value.Start, current, 0, len);
            position    = len;
            return new JsonValue(current, 0, len);
        }
    }
}