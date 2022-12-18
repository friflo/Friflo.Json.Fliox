// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Utils
{
    public class TestMessageBuffer
    {
        [Test]
        public async Task TestMessageBufferQueue() {
            var queue = new MessageBufferQueueAsync<VoidMeta>(5);
            
            var msg1 = new JsonValue("msg-1");
            var msg2 = new JsonValue("msg-2");
            var msg3 = new JsonValue("msg-3");
            
            queue.Enqueue(msg1);
            queue.Enqueue(msg2);
            
            var messages    = new List<MessageItem<VoidMeta>>();
            var ev          = await queue.DequeMessagesAsync(messages);
            
            queue.Enqueue(msg3);

            AreEqual(2, messages.Count);
            AreEqual("msg-1", messages[0].value.AsString());
            AreEqual("msg-2", messages[1].value.AsString());
            AreEqual(MessageBufferEvent.NewMessage, ev);
            
            ev = await queue.DequeMessagesAsync(messages);
            
            AreEqual(1, messages.Count);
            AreEqual("msg-3", messages[0].value.AsString());
            AreEqual(MessageBufferEvent.NewMessage, ev);
        }
        
        [Test]
        public async Task TestMessageBufferQueueClose() {
            var queue = new MessageBufferQueueAsync<VoidMeta>(2);
            
            var msg1 = new JsonValue("msg-1");
            
            queue.Enqueue(msg1);
            queue.Close();
            
            var messages    = new List<MessageItem<VoidMeta>>();
            var ev          = await queue.DequeMessagesAsync(messages);
            
            AreEqual(1, messages.Count);
            AreEqual("msg-1", messages[0].value.AsString());
            AreEqual(MessageBufferEvent.Closed, ev);
        }
        
        [Test]
        public async Task TestMessageBufferWait() {
            var queue = new MessageBufferQueueAsync<VoidMeta>(2);
            
            var messages = new List<MessageItem<VoidMeta>>();
            var waitTask = queue.DequeMessagesAsync(messages);
            
            var msg1 = new JsonValue("msg-1");
            var msg2 = new JsonValue("msg-2");
            
            queue.Enqueue(msg1);
            
            var ev = await waitTask;
            
            queue.Enqueue(msg2);
            
            AreEqual(1, messages.Count);
            AreEqual("msg-1", messages[0].value.AsString());
            AreEqual(MessageBufferEvent.NewMessage, ev);
        }
        
        [Test]
        public async Task TestMessageBufferConcurrent() {
            var queue       = new MessageBufferQueueAsync<VoidMeta>(2);
            var duration    = 10;
            var bulkSize    = 1000;
            
            var thread = new Thread(() =>
            {
                try {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var index = 0;
                    for (int n = 0; n < duration; n++) {
                        for (int i = 0; i < bulkSize; i++) {
                            var msg = new JsonValue($"{index++}");
                            queue.Enqueue(msg);
                        }
                        while (stopwatch.ElapsedMilliseconds < n) { }
                    }
                    queue.Close();
                }
                catch (Exception e) {
                    Fail(e.Message);
                }
            });
            thread.Start();

            int messageIndex    = 0;
            int dequeCount      = 0;
            while (true) {
                var messages    = new List<MessageItem<VoidMeta>>();
                var ev          = await queue.DequeMessagesAsync(messages);
                dequeCount++;
                // Console.WriteLine($"{count} - messages: {messages.Count}");
                foreach (var msg in messages) {
                    int.TryParse(msg.value.AsString(), out int value);
                    if (value != messageIndex) throw  new InvalidOperationException($"Expect {messageIndex}, was {value}");
                    messageIndex++;
                }
                if (ev == MessageBufferEvent.Closed) {
                    Console.WriteLine($"Finished messages: {messageIndex}, dequeues: {dequeCount}");
                    AreEqual(duration * bulkSize, messageIndex);
                    return;
                }
            }
        }
    }
}