// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Utils
{
    public class TestMessageBuffer
    {
        [Test]
        public void TestMessageBufferQueue() {
            var queue = new MessageBufferQueue<VoidMeta>(5);
            
            var msg1 = new JsonValue("msg-1");
            var msg2 = new JsonValue("msg-2");
            var msg3 = new JsonValue("msg-3");
            
            queue.AddTail(msg1);
            queue.AddTail(msg2);
            {
                int count = 0;
                foreach (var item in queue) {
                    count++;
                    if (count == 1) AreEqual("msg-1", item.value.AsString());
                    if (count == 2) AreEqual("msg-2", item.value.AsString());
                }
                AreEqual(2, count);
            }
            var messages    = new List<MessageItem<VoidMeta>>();
            var ev          = queue.DequeMessages(messages);
            
            queue.AddTail(msg3);
            {
                int count = 0;
                foreach (var item in queue) {
                    count++;
                    if (count == 1) AreEqual("msg-3", item.value.AsString());
                }
                AreEqual(1, count);
            }
            AreEqual(2, messages.Count);
            AreEqual("msg-1", messages[0].value.AsString());
            AreEqual("msg-2", messages[1].value.AsString());
            AreEqual(MessageBufferEvent.NewMessage, ev);

            ev = queue.DequeMessages(messages);
            
            AreEqual(1, messages.Count);
            AreEqual("msg-3", messages[0].value.AsString());
            AreEqual(MessageBufferEvent.NewMessage, ev);
            {
                int count = 0;
                foreach (var _ in queue) {
                    count++;
                }
                AreEqual(0, count);
            }
        }
        
        [Test]
        public void TestMessageBuffer_ClearAlloc() {
            var queue = new MessageBufferQueue<int>(5);
            var msg1 = new JsonValue("msg-1");
            var msg2 = new JsonValue("msg-2");
            
            long start = -1;
            for (int n = 0; n < 10; n++) {
                if (n == 2) {
                    start = GC.GetAllocatedBytesForCurrentThread();
                }
                queue.AddTail(msg1, 1);
                queue.AddHead(msg2, 2);
                queue.Clear();
                if (queue.Count != 0) throw new InvalidCastException("Expect Count == 0");
            }
            var dif  = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual(0, dif);
        }
        
        [Test]
        public void TestMessageBuffer_AddHeadQueue() {
            var queue = new MessageBufferQueue<int>(5);
            var msg3 = new JsonValue("msg-3");
            var msg4 = new JsonValue("msg-4");
            queue.AddTail(msg3, 3);
            queue.AddTail(msg4, 4);
            
            var prepend = new MessageBufferQueue<int>(5);
            var msg1 = new JsonValue("msg-1");
            var msg2 = new JsonValue("msg-2");
            
            prepend.AddTail(msg1, 1);
            prepend.AddTail(msg2, 2);
            
            queue.AddHeadQueue(prepend);

            int count = 0;
            foreach (var message in queue) {
                count++;
                AreEqual(count, message.meta);
            }
            AreEqual(4, count);
        }
    }
}