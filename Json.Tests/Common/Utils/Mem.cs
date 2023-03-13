// Copyright (c) Ullrich Praetz. All rights reserved.  
// See LICENSE file in the project root for full license information.


using System;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Tests.Common
{
    /// <summary>
    /// Introduced since TargetFrameworks <b>netstandard2.0</b> does not provide<br/>
    /// <c>GC.GetAllocatedBytesForCurrentThread()</c>
    /// </summary>
    public static class Mem
    {
        public static long GetAllocatedBytes() {
#if NET6_0_OR_GREATER
            return GC.GetAllocatedBytesForCurrentThread();
#else
            return 0;
#endif
        }
    }
}