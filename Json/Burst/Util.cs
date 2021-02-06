// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
        public          object      resource;
        public readonly StackTrace  stackTrace;

        public Allocation(object resource, StackTrace stackTrace) {
            this.resource = resource;
            this.stackTrace = stackTrace;
        }

        public override bool Equals(object obj) {
            // ReSharper disable once PossibleNullReferenceException
            return resource == ((Allocation)obj).resource;
        }

        public override int GetHashCode() {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return RuntimeHelpers.GetHashCode(resource);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public enum AllocType {
        Temp,
        Persistent
    }
#if JSON_BURST
    public struct AllocUtils
    {
        public static Unity.Collections.Allocator AsAllocator(AllocType allocType) {
            switch (allocType) {
                case AllocType.Persistent:
                    return Unity.Collections.Allocator.Persistent;
                case AllocType.Temp:
                    return Unity.Collections.Allocator.Temp;
            }
            // unreachable
            return default;
        }  
    }
#endif


    public static class DebugUtils
    {
        public  static readonly Dictionary<Allocation, Allocation>  Allocations = new Dictionary<Allocation, Allocation>();
        private static readonly Allocation                          SearchKey   = new Allocation (null, null);
        
        private static bool _enableLeakDetection;
        
        public static void TrackAllocation(object resource) {
            if (!_enableLeakDetection)
                return;
            if (resource == null)
                throw new InvalidOperationException("null is not allowed for tracking");
            lock (Allocations) {
                var allocation = new Allocation(resource, new StackTrace(true));
                
                if (Allocations.TryGetValue(allocation, out Allocation oldStackTrace))
                    throw new InvalidOperationException("resource is already tracked. Old resource: " + oldStackTrace.stackTrace);

                Allocations.Add(allocation, allocation);
            }
        }

        public static void UntrackAllocation(object resource) {
            if (!_enableLeakDetection)
                return;
            lock (Allocations) {
                SearchKey.resource = resource;
                // Remove() returns true, if the resource was found and removed
                if (!Allocations.Remove(SearchKey))
                    throw new InvalidOperationException("untrack expect the resource was previously tracked");
            }
        }

        public static void StartLeakDetection() {
            _enableLeakDetection = true;
            lock (Allocations) {
                Allocations.Clear();
            }
        }
        
        public static void StopLeakDetection() {
            _enableLeakDetection = false;
        }

    }
}

 
#if UNITY_2020_1_OR_NEWER && !JSON_BURST
#error Burst mode disabled. If disabled this library cannot be used in Burst Jobs. Comment this line or enable Burst Jobs by adding C# preprocessor directive JSON_BURST to: Edit > Project Settings... > Player > Other Settings > Configuration > Scripting Define Symbols  
#endif