// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Threading;

namespace Friflo.Json.Fliox.Hub.Remote
{
    internal readonly struct RemoteMessage
    {
        internal readonly   byte[]  buffer;
        internal readonly   int     start;
        internal readonly   int     len;
        
        internal            bool    IsNull()        => buffer == null;
        public   override   string  ToString()      => AsString();
        internal            string  AsString()      => buffer == null ? "null" : Encoding.UTF8.GetString(buffer, start, len);
        public  ArraySegment<byte>  AsArraySegment()=> new ArraySegment<byte>(buffer, start, len);
        
        internal RemoteMessage(byte[] buffer, int start, int len) {
            this.buffer = buffer;
            this.start  = start;
            this.len    = len;
        }
    }
    
    internal enum RemoteEvent {
        Closed      = 1,
        NewMessage  = 2,
    }
    
    internal class RemoteMessageQueue : IDisposable
    {
        private             byte[]                  buffer;
        private             int                     bufferLen;
        private             int                     bufferPos;
        private             byte[]                  newBuffer;
        private             int                     newBufferPos;
        private  readonly   List<RemoteMessage>     queue;
        
        private  readonly   DataChannelSlim   <RemoteEvent>   channel;
        private  readonly   IDataChannelWriter<RemoteEvent>   sendWriter;
        private  readonly   IDataChannelReader<RemoteEvent>   sendReader;
        
        internal RemoteMessageQueue() {
            bufferLen   = 128;
            buffer      = new byte[bufferLen];
            queue       = new List<RemoteMessage>();
            
            channel     = DataChannelSlim<RemoteEvent>.CreateUnbounded(true, false);
            sendWriter  = channel.Writer;
            sendReader  = channel.Reader;
        }

        public void Dispose() {
            channel.Dispose();
        }
        
        internal RemoteMessage Enqueue(in JsonValue data) {
            return Enqueue(data.GetArrayMutable(), 0, data.Length);
        }
        
        internal RemoteMessage Enqueue(in Bytes bytes) {
            return Enqueue(bytes.buffer.array, bytes.start, bytes.Len);
        }

        private RemoteMessage Enqueue(byte[] data, int start, int len) {
            lock (queue) {
                var remaining = bufferLen - bufferPos;
                if (len > remaining) {
                    bufferLen   = Math.Max(2 * bufferLen, len);
                    buffer      = new byte[bufferLen];
                    bufferPos   = 0;
                }
                Buffer.BlockCopy(data, start, buffer, bufferPos, len);
                var message = new RemoteMessage(buffer, bufferPos, len);
                queue.Add(message);
                bufferPos += len;
                
                // send event _after_ adding message to queue
                sendWriter.TryWrite(RemoteEvent.NewMessage);
                return message;
            }
        }
        
        internal async Task<RemoteEvent> DequeMessages(List<RemoteMessage> messages)
        {
            messages.Clear();
            var messageEvent = await sendReader.ReadAsync().ConfigureAwait(false);
            if (messageEvent == RemoteEvent.Closed) {
                return messageEvent;
            }
            lock (queue) {
                foreach (var message in queue) {
                    messages.Add(message);                    
                }
                queue.Clear();
                newBuffer       = buffer;
                newBufferPos    = bufferPos;
            }
            return messageEvent;
        }
        
        internal void FreeDequeuedMessages() {
            lock (queue) {
                var queueCount = queue.Count;
                // early out if no new messages added meanwhile
                if (queueCount == 0) {
                    return;
                }
                // early out if buffer changed - caused by the need of a bigger one
                if (buffer != newBuffer) {
                    return;
                }
                // move newly added messages to begin of buffer
                int addedLen = 0; // length of all newly added messages
                for (int n = 0; n < queueCount; n++) {
                    var message = queue[n];
                    // adjust start position only if using the same shared buffer
                    if (message.buffer != newBuffer)
                        continue;
                    queue[n]  = new RemoteMessage (newBuffer, message.start - newBufferPos, message.len);
                    addedLen += message.len;
                }
                Buffer.BlockCopy(newBuffer, newBufferPos, newBuffer, 0, addedLen);
                bufferPos       = addedLen;
                newBuffer       = null;
                newBufferPos    = 0;
            }
        }

        public void Close() {
            sendWriter.TryWrite(RemoteEvent.Closed);
            sendWriter.Complete();
        }
    }
}