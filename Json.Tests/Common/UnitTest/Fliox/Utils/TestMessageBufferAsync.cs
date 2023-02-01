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
    public class TestMessageBufferAsync
    {
        [Test]
        public async Task TestMessageBufferQueueAsync() {
            var queue = new MessageBufferQueueAsync<VoidMeta>(5);
            
            var msg1 = new JsonValue("msg-1");
            var msg2 = new JsonValue("msg-2");
            var msg3 = new JsonValue("msg-3");
            
            queue.AddTail(msg1);
            queue.AddTail(msg2);
            
            var messages    = new List<JsonValue>();
            var ev          = await queue.DequeMessageValuesAsync(messages);
            
            queue.AddTail(msg3);

            AreEqual(2, messages.Count);
            AreEqual("msg-1", messages[0].AsString());
            AreEqual("msg-2", messages[1].AsString());
            AreEqual(MessageBufferEvent.NewMessage, ev);
            
            ev = await queue.DequeMessageValuesAsync(messages);
            
            AreEqual(1, messages.Count);
            AreEqual("msg-3", messages[0].AsString());
            AreEqual(MessageBufferEvent.NewMessage, ev);
        }
        
        [Test]
        public async Task TestMessageBufferQueueAsyncClose() {
            var queue = new MessageBufferQueueAsync<VoidMeta>(2);
            
            var msg1 = new JsonValue("msg-1");
            
            queue.AddTail(msg1);
            queue.Close();
            
            var messages    = new List<JsonValue>();
            var ev          = await queue.DequeMessageValuesAsync(messages);
            
            AreEqual(1, messages.Count);
            AreEqual("msg-1", messages[0].AsString());
            AreEqual(MessageBufferEvent.Closed, ev);
        }
        
        [Test]
        public async Task TestMessageBufferQueueAsyncWait() {
            var queue = new MessageBufferQueueAsync<VoidMeta>(2);
            
            var messages = new List<JsonValue>();
            var waitTask = queue.DequeMessageValuesAsync(messages);
            
            var msg1 = new JsonValue("msg-1");
            var msg2 = new JsonValue("msg-2");
            
            queue.AddTail(msg1);
            
            var ev = await waitTask;
            
            queue.AddTail(msg2);
            
            AreEqual(1, messages.Count);
            AreEqual("msg-1", messages[0].AsString());
            AreEqual(MessageBufferEvent.NewMessage, ev);
        }
        
        [Test]
        public async Task TestMessageBufferAsyncConcurrent() {
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
                            queue.AddTail(msg);
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
                var messages    = new List<JsonValue>();
                var ev          = await queue.DequeMessageValuesAsync(messages);
                dequeCount++;
                // Console.WriteLine($"{count} - messages: {messages.Count}");
                foreach (var msg in messages) {
                    int.TryParse(msg.AsString(), out int value);
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