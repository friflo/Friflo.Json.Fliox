// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary> Reflect the shape of a <see cref="EventMessage"/> </summary>
    public struct RemoteEventMessage
    {
        /** map to <see cref="ProtocolEvent"/> discriminator */ public  string                  msg;
        /** map to <see cref="ProtocolEvent.dstClientId"/> */   public  JsonKey                 clt;
        /** map to <see cref="EventMessage.events"/> */         public  List<RemoteSyncEvent>   events;
    }
    
    /// <summary> Reflect the shape of a <see cref="SyncEvent"/> </summary>
    public struct RemoteSyncEvent
    {
        /** map to <see cref="SyncEvent.seq"/> */               public  int         seq; 
        /** map to <see cref="SyncEvent.srcUserId"/> */         public  JsonKey     src;
        /** map to <see cref="SyncEvent.db"/> */                public  string      db;
        /** map to <see cref="SyncEvent.isOrigin"/> */          public  bool?       isOrigin;
        /** map to <see cref="SyncEvent.tasks"/> */             public  JsonValue[] tasks;
    }
    
    public static class RemoteUtils
    {
        public static JsonValue CreateProtocolMessage (ProtocolMessage message, ObjectPool<ObjectMapper> objectMapper)
        {
            using (var pooled = objectMapper.Get()) {
                var mapper = pooled.instance;
                mapper.Pretty           = true;
                mapper.WriteNullMembers = false;
                return mapper.WriteAsValue(message);
            }
        }
        
        public static Bytes CreateProtocolEvent (EventMessage eventMessage, in SendEventArgs args)
        {
            var mapper              = args.mapper;
            mapper.Pretty           = true;
            mapper.WriteNullMembers = false;
            if (!EventDispatcher.SerializeRemoteEvents) {
                return mapper.writer.WriteAsBytes(eventMessage);
            }
            var remoteEventMessage      = new RemoteEventMessage { msg = "ev", clt = eventMessage.dstClientId };
            var events                  = eventMessage.events;
            var remoteEvents            = args.eventBuffer;
            remoteEvents.Clear();
            remoteEventMessage.events   = remoteEvents;
            for (int n = 0; n < events.Count; n++) {
                var ev = events[n];
                var remoteEv = new RemoteSyncEvent {
                    seq         = ev.seq,
                    src         = ev.srcUserId,
                    db          = ev.db,
                    isOrigin    = ev.isOrigin,
                    tasks       = ev.tasksJson,
                };
                remoteEvents.Add(remoteEv);
            }
            return mapper.writer.WriteAsBytes(remoteEventMessage);
        }
        
        private static readonly byte[] DiscriminatorKey     = Encoding.UTF8.GetBytes("msg");
        private static readonly byte[] DiscriminatorValue   = Encoding.UTF8.GetBytes("sync");
        
        public static SyncRequest ReadSyncRequest (
            InstancePool                instancePool,
            in JsonValue                jsonMessage,
            ObjectPool<ObjectMapper>    mapperPool,
            out string                  error)
        {
            using (var pooledMapper = mapperPool.Get()) {
                var reader  = pooledMapper.instance.reader;
                var message = reader.Read<ProtocolMessage>(jsonMessage);
                if (reader.Error.ErrSet) {
                    error = reader.Error.GetMessage();
                    return null;
                }
                if (message is SyncRequest syncRequest) {
                    error = null;
                    return syncRequest;
                }
                error = $"Expect 'sync' request. was: '{message.MessageType}'";
                return null;
            }
        }
        
        public static ProtocolMessage ReadProtocolMessage (in JsonValue jsonMessage, ObjectPool<ObjectMapper> mapperPool, out string error)
        {
            using (var pooledMapper = mapperPool.Get()) {
                ObjectReader reader = pooledMapper.instance.reader;
                var message         = reader.Read<ProtocolMessage>(jsonMessage);
                if (reader.Error.ErrSet) {
                    error = reader.Error.msg.ToString();
                    return null;
                }
                error = null;
                return message;
            }
        }
    }
}