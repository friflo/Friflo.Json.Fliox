// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Friflo.Json.Burst.Utils
{
    public static class UnsafeUtils
    {
        public static unsafe Span<T> CreateSpan<T>(ref T value) {
            void* valPtr = Unsafe.AsPointer(ref value);
            return new Span<T>(valPtr, Marshal.SizeOf<T>());
        }
        
        public static unsafe ReadOnlySpan<T> CreateReadOnlySpan<T>(ref T value) {
            void* valPtr = Unsafe.AsPointer(ref value);
            return new ReadOnlySpan<T>(valPtr, Marshal.SizeOf<T>());
        }
    }
}