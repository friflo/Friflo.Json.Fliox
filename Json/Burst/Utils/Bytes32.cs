// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Friflo.Json.Burst.Utils
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Bytes32
    {
        private ulong byte00;
        private ulong byte08;
        private ulong byte16;
        private ulong byte24;

        private int len;
        
        
        private const ulong Mask = 0xffffffffffffffff;
        private const ulong Zero = 0L;

        public unsafe void FromBytes (in Bytes str, Untracked _ = Untracked.Bytes) {
            int start   = str.start;
            len         = str.end - start;
            if (str.buffer.Length < start + 32)
                throw new InvalidOperationException("FromBytes() - insufficient length");
            
            fixed (byte*  srcPtr  = &str.buffer[start])
            fixed (ulong* destPtr = &byte00)
            {
                if (len <= 8) {
                    *(destPtr + 0) = *(ulong*)(srcPtr +  0) & Mask >> ((8 - len) << 3);
                    *(destPtr + 1) = Zero;
                    *(destPtr + 2) = Zero;
                    *(destPtr + 3) = Zero;
                }
                else if (len <= 16) {
                    *(destPtr + 0) = *(ulong*)(srcPtr +  0);
                    *(destPtr + 1) = *(ulong*)(srcPtr +  8) & Mask >> ((16 - len) << 3);
                    *(destPtr + 2) = Zero;
                    *(destPtr + 3) = Zero;
                }
                else if (len <= 24) {
                    *(destPtr + 0) = *(ulong*)(srcPtr +  0);
                    *(destPtr + 1) = *(ulong*)(srcPtr +  8);
                    *(destPtr + 2) = *(ulong*)(srcPtr + 16) & Mask >> ((24 - len) << 3);
                    *(destPtr + 3) = Zero;
                }
                else if (len <= 32) {
                    *(destPtr + 0) = *(ulong*)(srcPtr +  0);
                    *(destPtr + 1) = *(ulong*)(srcPtr +  8);
                    *(destPtr + 2) = *(ulong*)(srcPtr + 16);
                    *(destPtr + 3) = *(ulong*)(srcPtr + 24) & Mask >> ((32 - len) << 3);
                }
                else {
                    len = 32;
                    *(destPtr + 0) = *(ulong*)(srcPtr +  0);
                    *(destPtr + 1) = *(ulong*)(srcPtr +  8);
                    *(destPtr + 2) = *(ulong*)(srcPtr + 16);
                    *(destPtr + 3) = *(ulong*)(srcPtr + 24);
                }
            }
        }
        
        public unsafe void ToBytes (ref Bytes str) {
            if (str.buffer.Length < 32)
                str.EnsureCapacityAbs(32);
            str.start = 0;
            str.end = len;
            
            fixed (byte*  destPtr = &str.buffer[0])
            fixed (ulong* srcPtr  = &byte00)
            {
                *(ulong*)(destPtr +  0) = *(srcPtr + 0);
                *(ulong*)(destPtr +  8) = *(srcPtr + 1);
                *(ulong*)(destPtr + 16) = *(srcPtr + 2);
                *(ulong*)(destPtr + 24) = *(srcPtr + 3);
            }
        }

        public override string ToString() {
            var str = new Bytes(32);
            ToBytes(ref str);
            return str.ToString();
        }

        public bool IsEqual(ref Bytes32 other) {
            return byte00 == other.byte00 && byte08 == other.byte08 && byte16 == other.byte16 && byte24 == other.byte24;
        }
    }
}
