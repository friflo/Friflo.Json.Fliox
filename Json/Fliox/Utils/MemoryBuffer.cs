// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Friflo.Json.Fliox.Utils
{
    public class MemoryBuffer
    {
        private     int     messageStart;
        private     int     capacity;
        private     int     position;
        private     byte[]  buffer;
        private     int     bufferVersion;
        
        public      int     Capacity        => capacity;
        /// <summary> <see cref="Capacity"/> - <see cref="Position"/> </summary>
        public      int     Remaining       => capacity - position;
        public      int     MessageStart    => messageStart;
        public      int     MessageLength   => position - messageStart; 
        public      byte[]  GetBuffer()     => buffer;
        public      int     BufferVersion   => bufferVersion;
        
        public      int     Position {
            get => position;
            set {
                if (value < messageStart)   throw new ArgumentException("expect position >= MessageStart");
                if (value > capacity)       throw new ArgumentException("expect position <= Capacity");
                position = value;
            }
        }
        
        public MemoryBuffer() : this (4096) { }
        
        public MemoryBuffer(int capacity) {
            this.capacity   = capacity;
            buffer          = new byte[capacity];
        }
        
        public void SetMessageStart() {
            messageStart = position;
        }
        
        /// <summary>Set new capacity of the internal buffer returned with <see cref="GetBuffer"/>.</summary>
        public void SetCapacity (int newCapacity) {
            if (capacity > newCapacity) throw new ArgumentException("expect new capacity > current Capacity");
            capacity        = newCapacity;
            NewBuffer();
        }
        
        public void NewBuffer () {
            var newBuffer   = new byte[capacity];
            Buffer.BlockCopy(buffer, messageStart, newBuffer, 0, MessageLength);
            position       -= messageStart;
            messageStart    = 0;
            buffer          = newBuffer;
            bufferVersion++;
        }
        
        public void AddReadSpace() {
            if (2 * MessageLength > capacity) {
                SetCapacity(2 * capacity);
                return;
            }
            // MessageLength < capacity / 2   =>  still enough remaining read buffer space
            NewBuffer();
        }
        
        public byte[] CreateMessageArray() {
            var len     = MessageLength;
            var result  = new byte[len];
            Buffer.BlockCopy(buffer, messageStart, result, 0, len);
            return result;
        }
    }
}