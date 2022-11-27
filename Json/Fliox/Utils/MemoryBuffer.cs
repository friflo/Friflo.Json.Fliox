// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Utils
{
    /// <summary>
    /// Used to allocate byte arrays used within <see cref="JsonValue"/>'s on the heap. <br/>
    /// In general it tries to minimize the amount of array allocations.
    /// </summary>
    /// <remarks>
    /// Strategy to minimize heap allocations and GC pressure.
    /// <list type="bullet">
    ///   <item>don't allocate any array if the <see cref="MemoryBuffer"/> is not reused</item>
    ///   <item>store only a single <see cref="permanent"/> buffer if <see cref="MemoryBuffer"/> is reused</item>
    ///   <item>avoid allocation of 'large heap object' <see cref="buffer"/>'s which required a GEN2 collection</item>
    ///   <item>avoid allocated <see cref="buffer"/>'s having too many unused remaining bytes => quadruple capacity</item>
    /// </list>
    /// </remarks>
    public class MemoryBuffer : IDisposable
    {
        private readonly    byte[]  permanent;
        private             byte[]  buffer;
        private             int     position;
        private readonly    int     initialCapacity;
        private             int     capacity;
        // --- stats
        private             int     bigSize;
        private             int     bigCount;
        private             int     smallSize;
        private             int     smallCount;

        public  override    string  ToString() => $"count {bigCount + smallCount}, size: {bigSize + smallSize}";

        // [Large object heap (LOH) on Windows | Microsoft Learn] https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap
        private const       int     LargeHeapObjectSize = 84000;                    // large object size: 85_000. Use a size smaller than this
        private const       int     BigValueLength      = LargeHeapObjectSize / 4;  // all arrays with Length > this get an individual array allocated

        public MemoryBuffer(bool reuse, int initialCapacity) {
            this.initialCapacity    =
            capacity                = initialCapacity;
            permanent               = reuse ? new byte[initialCapacity] : null;
        }
        
        public void Dispose() { }
        
        public void Reset() {
            capacity    = initialCapacity;
            buffer      = permanent;
            position    = permanent == null ? capacity : 0;
            bigSize     = 0;
            bigCount    = 0;
            smallSize   = 0;
            smallCount  = 0;
        }

        /// <summary> add the <paramref name="value"/> to the <see cref="MemoryBuffer"/> </summary>
        public JsonValue Add(in JsonValue value) {
            int len          = value.Count;
            // value size beyond limits => create an individual copy
            if (len >= BigValueLength) {
                bigSize     += len;
                bigCount    ++;
                return new JsonValue(value);
            }
            smallSize  += len;
            smallCount ++;
            int start   = position;
            if (start + len <= capacity) {
                // add value to current buffer
                Buffer.BlockCopy(value.Array, value.Start, buffer, start, len);
                position += len;
                return new JsonValue(buffer, start, len);
            }
            capacity    = 4 * Math.Max(len, capacity);              // quadruple current len / capacity to avoid allocation of too many unused remaining bytes
            capacity    = Math.Min(capacity, LargeHeapObjectSize);  // cap array size to LargeHeapObjectSize
            // create a new buffer and add value
            buffer     = new byte[capacity];
            Buffer.BlockCopy(value.Array, value.Start, buffer, 0, len);
            position    = len;
            return new JsonValue(buffer, 0, len);
        }
    }
}