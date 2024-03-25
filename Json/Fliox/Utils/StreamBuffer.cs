// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Friflo.Json.Fliox.Utils
{
    public sealed class StreamBuffer
    {
        private     int     capacity;
        private     int     position;
        private     byte[]  buffer;
        
        public      int     Capacity        => capacity;
        /// <summary> <see cref="Capacity"/> - <see cref="Position"/> </summary>
        public      int     Remaining       => capacity - position;
        public      byte[]  GetBuffer()     => buffer;
        
        public      int     Position {
            get => position;
            set {
                if (value > capacity)       throw new ArgumentException("expect position <= Capacity");
                position = value;
            }
        }
        
        public StreamBuffer() : this (4096) { }
        
        public StreamBuffer(int capacity) {
            this.capacity   = capacity;
            buffer          = new byte[capacity];
        }
        
        /// <summary>Set new capacity of the internal buffer returned with <see cref="GetBuffer"/>.</summary>
        public void SetCapacity (int newCapacity) {
            if (capacity > newCapacity) throw new ArgumentException("expect new capacity > current Capacity");
            capacity        = newCapacity;
            var newBuffer   = new byte[capacity];
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, position);
            buffer          = newBuffer;
        }
        
        public byte[] ToArray() {
            var result  = new byte[position];
            Buffer.BlockCopy(buffer, 0, result, 0, position);
            return result;
        }
    }
}