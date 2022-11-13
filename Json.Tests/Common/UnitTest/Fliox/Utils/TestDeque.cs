// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Utils
{
    public static class TestDeque
    {
        [Test]
        public static void TestDequeAddTail() {
            var deque = new Deque<int>(1);
            AssertDequeAddTail(deque); // increases capacity 
            
            deque = new Deque<int>(2);
            var start = GC.GetAllocatedBytesForCurrentThread();
            AssertDequeAddTail(deque);
            var dif = GC.GetAllocatedBytesForCurrentThread() - start;
            
            AreEqual(0, dif);
        }
        
        
        private static void AssertDequeAddTail(Deque<int> deque)
        {
            foreach (var unused in deque) { Fail("unexpected"); }
            
            // --- items: [1, 2]
            deque.AddTail(1);
            deque.AddTail(2);
            
            if (deque.Count != 2)       throw new InvalidOperationException($"unexpected Count {deque.Count}");

            int value = 0;
            foreach (var item in deque) {
                if (item != ++value)    throw new InvalidOperationException($"expect {value}");
            }
            if (value != 2)             throw new InvalidOperationException($"unexpected {value}");
            
            // --- items: [2]
            var removed = deque.RemoveHead();
            if (removed != 1)           throw new InvalidOperationException($"unexpected item: {removed}");
            if (deque.Count != 1)       throw new InvalidOperationException($"unexpected Count {deque.Count}");
            value = 1;
            foreach (var item in deque) {
                if (item != ++value)    throw new InvalidOperationException($"expect {value}");
            }
            if (value != 2)             throw new InvalidOperationException($"unexpected {value}");
            
            // --- items: []
            removed = deque.RemoveHead();
            if (removed != 2)           throw new InvalidOperationException($"unexpected item: {removed}");
            if (deque.Count != 0)       throw new InvalidOperationException($"unexpected Count {deque.Count}");
        }
        
        [Test]
        public static void TestDequeAddHead() {
            var deque = new Deque<int>(1);
            AssertAddHead(deque); // increases capacity
            
            deque = new Deque<int>(2);
            var start = GC.GetAllocatedBytesForCurrentThread();
            AssertAddHead(deque);
            var dif = GC.GetAllocatedBytesForCurrentThread() - start;
            
            AreEqual(0, dif);
        }

        private static void AssertAddHead(Deque<int> deque)
        {
            foreach (var unused in deque) { Fail("unexpected"); }
            
            // --- items: [1, 2]
            deque.AddHead(2);
            deque.AddHead(1);
            
            if (deque.Count != 2)       throw new InvalidOperationException($"unexpected Count {deque.Count}");

            int value = 0;
            foreach (var item in deque) {
                if (item != ++value)    throw new InvalidOperationException($"expect {value}");
            }
            if (value != 2)             throw new InvalidOperationException($"unexpected {value}");
            
            // --- items: [2]
            var removed = deque.RemoveHead();
            if (removed != 1)           throw new InvalidOperationException($"unexpected item: {removed}");
            if (deque.Count != 1)       throw new InvalidOperationException($"unexpected Count {deque.Count}");
            value = 1;
            foreach (var item in deque) {
                if (item != ++value)    throw new InvalidOperationException($"expect {value}");
            }
            if (value != 2)             throw new InvalidOperationException($"unexpected {value}");
            
            // --- items: []
            removed = deque.RemoveHead();
            if (removed != 2)           throw new InvalidOperationException($"unexpected item: {removed}");
            if (deque.Count != 0)       throw new InvalidOperationException($"unexpected Count {deque.Count}");
        }
    }
}