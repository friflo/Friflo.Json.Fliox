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
        }
        
        public void Dispose() { }
        
        public void Reset() {
            if (permanent != null) {
                current     = permanent;
                position    = 0;
                return;
            }
            current     = null;
            position    = capacity;
        }

        /// <summary> add the <paramref name="value"/> to the <see cref="MemoryBuffer"/> </summary>
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