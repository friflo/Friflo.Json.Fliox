// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


#if DEBUG
using System.Collections.Generic;
using System.Diagnostics;
#endif

namespace Friflo.Json.Burst
{
    /**
     * Used to implement a default constructor for a struct's as C# cant have parameter less constructors for structs right now. 
     */
    public enum Default {
        Constructor // never used 
    }

    public class Allocation
    {
        public object resource;
        public StackTrace stackTrace;
    }

    public static class DebugUtils
    {
        public static Dictionary<object, StackTrace> allocations = new Dictionary<object, StackTrace>();
        
        public static void AcquireAllocation(object resource) {
#if DEBUG
            var allocation = new Allocation();
            allocation.resource = resource;
            StackTrace stackTrace = new StackTrace(true);
            // StackFrame[] stackFrames = stackTrace.GetFrames();
            allocations.Add(resource, stackTrace);
#endif
        }

        public static void ReleaseAllocation(object resource) {
            allocations.Remove(resource);
        }
    }

}