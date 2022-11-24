// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Friflo.Json.Fliox.Utils
{
    public class MemoryBuffer
    {
        private     int     messageStart;
        private     int     capacity;
        private     int     position;
        private     byte[]  buffer;
        
        public      int     Capacity        => capacity;
        public      int     MessageStart    => messageStart;
        public      int     MessageLength   => position - messageStart; 
        public      byte[]  GetBuffer()     => buffer;
        
        public      int     Position {
            get => position;
            set {
                if (value < messageStart)   throw new ArgumentException("expect position >= MessageStart");
                if (value > capacity)       throw new ArgumentException("expect position <= Capacity");
                position = value;
            }
        }
        
        public MemoryBuffer(int capacity) {
            this.capacity   = capacity;
            buffer          = new byte[capacity];
        }
        
        public void SetMessageStart() {
            messageStart = position; 
        }
        
        public void SetCapacity (int newCapacity) {
            if (capacity >= newCapacity) throw new ArgumentException("expect new capacity >= current Capacity");
            capacity        = newCapacity;
            var newBuffer   = new byte[capacity];
            Buffer.BlockCopy(buffer, messageStart, newBuffer, 0, position - messageStart);
            position       -= messageStart;
            messageStart    = 0;
            buffer          = newBuffer;
        }
    }
}