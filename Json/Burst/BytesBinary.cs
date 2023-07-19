// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Burst
{
    public partial struct Bytes
    {
        public unsafe void WriteInt32(int pos, int value)
        {
            fixed (byte* destPtr = &buffer [pos])
            {
                *(int*)(destPtr +  0) = value;
            }
        }
        
        public unsafe void WriteInt64(int pos, long value)
        {
            fixed (byte* destPtr = &buffer [pos])
            {
                *(long*)(destPtr +  0) = value;
            }
        }
        
        public unsafe void WriteFlt32(int pos, float value)
        {
            fixed (byte* destPtr = &buffer [pos])
            {
                *(float*)(destPtr +  0) = value;
            }
        }
        
        public unsafe void WriteFlt64(int pos, double value)
        {
            fixed (byte* destPtr = &buffer [pos])
            {
                *(double*)(destPtr +  0) = value;
            }
        }
        
        public unsafe void WriteLongLong(int pos, long value1, long value2)
        {
            fixed (byte* destPtr = &buffer [pos])
            {
                *(long*)(destPtr +  0) = value1;
                *(long*)(destPtr +  8) = value2;
            }
        }
        
        // --------------------------------------- read --------------------------------------- 
        public unsafe int ReadInt32(int pos)
        {
            fixed (byte* destPtr = &buffer [pos])
            {
                return  *(int*)(destPtr);
            }
        }
        
        public unsafe long ReadInt64(int pos)
        {
            fixed (byte* destPtr = &buffer [pos])
            {
                return *(long*)(destPtr);
            }
        }
        
        public unsafe float ReadFlt32(int pos)
        {
            fixed (byte* destPtr = &buffer [pos])
            {
                return  *(float*)(destPtr);
            }
        }
        
        public unsafe double ReadFlt64(int pos)
        {
            fixed (byte* destPtr = &buffer [pos])
            {
                return *(double*)(destPtr);
            }
        }
    }
}