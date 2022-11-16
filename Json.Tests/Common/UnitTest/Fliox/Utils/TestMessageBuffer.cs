// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
            var queue = new MessageBufferQueue();
            
            var msg1 = new JsonValue("msg-1");
            var msg2 = new JsonValue("msg-2");
            var msg3 = new JsonValue("msg-3");
            
            queue.Enqueue(msg1);
            queue.Enqueue(msg2);
            
            var messages = new List<MessageBuffer>();
            var ev = await queue.DequeMessages(messages);
            
            queue.Enqueue(msg3);

            AreEqual(2, messages.Count);
            AreEqual("msg-1", messages[0].AsString());
            AreEqual("msg-2", messages[1].AsString());
            AreEqual(MessageBufferEvent.NewMessage, ev);
            
            queue.FreeDequeuedMessages();
            
            ev = await queue.DequeMessages(messages);
            
            AreEqual(1, messages.Count);
            AreEqual("msg-3", messages[0].AsString());
            AreEqual(MessageBufferEvent.NewMessage, ev);
            
            queue.FreeDequeuedMessages();
        }
        
        [Test]
        public async Task TestMessageBufferQueueClose() {
            var queue = new MessageBufferQueue();
            
            var msg1 = new JsonValue("msg-1");
            
            queue.Enqueue(msg1);
            queue.Close();
            
            var messages    = new List<MessageBuffer>();
            var ev          = await queue.DequeMessages(messages);
            
            AreEqual(1, messages.Count);
            AreEqual("msg-1", messages[0].AsString());
            AreEqual(MessageBufferEvent.Closed, ev);
            
            queue.FreeDequeuedMessages();
        }
        
        [Test]
        public async Task TestMessageBufferWait() {
            var queue = new MessageBufferQueue();
            
            var messages = new List<MessageBuffer>();
            var waitTask = queue.DequeMessages(messages);
            
            var msg1 = new JsonValue("msg-1");
            var msg2 = new JsonValue("msg-2");
            
            queue.Enqueue(msg1);
            queue.Enqueue(msg2);
            
            var ev = await waitTask;
            
            AreEqual(2, messages.Count);
            AreEqual("msg-1", messages[0].AsString());
            AreEqual("msg-2", messages[1].AsString());
            AreEqual(MessageBufferEvent.NewMessage, ev);
            
            queue.FreeDequeuedMessages();
        }
    }
}