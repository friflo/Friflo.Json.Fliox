// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

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

    public class DebugUtils
    {
        public static Dictionary<object, StackTrace> allocations = new Dictionary<object, StackTrace>();
        private static bool enableLeakDetection = false;
        
        public static void TrackAllocation(object resource) {
            if (!enableLeakDetection)
                return;
            var allocation = new Allocation();
            allocation.resource = resource;
            StackTrace stackTrace = new StackTrace(true);
            // StackFrame[] stackFrames = stackTrace.GetFrames();
            allocations.Add(resource, stackTrace);
        }

        public static void UntrackAllocation(object resource) {
            allocations.Remove(resource);
        }

        public static void StartLeakDetection() {
            enableLeakDetection = true;
            allocations.Clear();
        }
        
        public static void StopLeakDetection() {
            enableLeakDetection = false;
        }

    }

}

 
#if UNITY_5_3_OR_NEWER && !JSON_BURST
#warning Burst mode disabled. To enable add directive JSON_BURST to: Edit > Project Settings... > Player > Other Settings > Configuration > Scripting Define Symbols  
#endif