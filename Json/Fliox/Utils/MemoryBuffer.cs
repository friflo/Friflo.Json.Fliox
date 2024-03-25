// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
    ///   <item>don't allocate any array if the <see cref="MemoryBuffer"/> is not used</item>
    ///   <item>avoid allocation of 'large heap object' <see cref="buffer"/>'s which required a GEN2 collection</item>
    ///   <item>avoid allocated <see cref="buffer"/>'s having too many unused remaining bytes => quadruple capacity</item>
    /// </list>
    /// </remarks>
    public sealed class MemoryBuffer : IDisposable
    {
        private             byte[]  startBuffer;
        private             byte[]  buffer;
        private             int     position;
        private readonly    int     startCapacity;
        private             int     capacity;
        // --- stats
        private             int     allocatedSize;
        private             int     allocatedCount;

        public  override    string  ToString() => $"count {allocatedCount}, size: {allocatedSize}";

        // [Large object heap (LOH) on Windows | Microsoft Learn] https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap
        private const       int     LargeHeapObjectSize = 84000;                    // large object size: 85_000. Use a size smaller than this
        private const       int     BigValueLength      = LargeHeapObjectSize / 4;  // all arrays with Length > this get an individual array allocated

        public MemoryBuffer(int startCapacity) {
            this.startCapacity  =
            capacity            = startCapacity;
        }
        
        public void Dispose() { }
        
        public void Reset() {
            buffer          = startBuffer;
            position        = 0;
            capacity        = startCapacity;
            allocatedSize   = 0;
            allocatedCount  = 0;
        }

        /// <summary> add the <paramref name="value"/> to the <see cref="MemoryBuffer"/> </summary>
        public JsonValue Add(in JsonValue value) {
            int len         = value.Count;
            allocatedSize  += len;
            allocatedCount ++;            // value size beyond limits => create an individual copy
            if (len >= BigValueLength) {
                return new JsonValue(value);
            }
            return AddInternal(value.AsReadOnlySpan());
        }
        
        /// <summary> add the <paramref name="value"/> to the <see cref="MemoryBuffer"/> </summary>
        public JsonValue Add(in ReadOnlySpan<byte> value) {
            int len         = value.Length;
            allocatedSize  += len;
            allocatedCount ++;            // value size beyond limits => create an individual copy
            if (len >= BigValueLength) {
                return new JsonValue(value.ToArray());
            }
            return AddInternal(value);
        }
        
        private JsonValue AddInternal(in ReadOnlySpan<byte> value) {
            int start   = position;
            var len     = value.Length;
            if (start + len <= capacity) {
                // buffer == null  =>  MemoryBuffer was Reset()  =>  capacity == startCapacity
                if (buffer == null) {
                    buffer  = startBuffer = new byte[startCapacity];
                }
                // add value to current buffer
                var target  = new Span<byte> (buffer, start, len);
                value.CopyTo(target);
                position += len;
                return new JsonValue(buffer, start, len);
            } else {
                capacity    = 4 * Math.Max(len, capacity);              // quadruple current len / capacity to avoid allocation of too many unused remaining bytes
                capacity    = Math.Min(capacity, LargeHeapObjectSize);  // cap array size to LargeHeapObjectSize
                // create a new buffer and add value
                buffer      = new byte[capacity];
                var target  = new Span<byte> (buffer, 0, len);
                value.CopyTo(target);
                position    = len;
                return new JsonValue(buffer, 0, len);
            }
        }
        
        public JsonValue GetValue(int position, int count) {
            if (position + count > this.position) throw new IndexOutOfRangeException($"position: {position}, max: {this.position}");
            return new JsonValue(buffer, position, count);
        }
    }
}