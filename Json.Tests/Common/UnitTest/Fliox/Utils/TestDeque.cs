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
        public static void TestDequeIteration() {
            var deque = new Deque<int>();
            var start = GC.GetAllocatedBytesForCurrentThread();
            
            foreach (var unused in deque) {
                Fail("unexpected");
            }
            
            // --- items: [1, 2]
            deque.AddTail(1);
            deque.AddTail(2);
            
            if (deque.Count != 2) throw new InvalidOperationException($"unexpected Count {deque.Count}");

            int value = 0;
            foreach (var item in deque) {
                if (item != ++value)  throw new InvalidOperationException($"expect {value}");
            }
            if (value != 2) throw new InvalidOperationException($"unexpected {value}");
            
            // --- items: [2]
            var first = deque.RemoveHead();
            if (first != 1)         throw new InvalidOperationException($"unexpected item: {first}");
            if (deque.Count != 1)   throw new InvalidOperationException($"unexpected Count {deque.Count}");
            value = 1;
            foreach (var item in deque) {
                if (item != ++value)  throw new InvalidOperationException($"expect {value}");
            }
            if (value != 2) throw new InvalidOperationException($"unexpected {value}");
            
            // --- items: []
            deque.Clear();
            if (deque.Count != 0) throw new InvalidOperationException($"unexpected Count {deque.Count}");

            var dif = GC.GetAllocatedBytesForCurrentThread() - start;
            
            AreEqual(0, dif);
        }
    }
}