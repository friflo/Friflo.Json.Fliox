// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary> Reflect the shape of a <see cref="EventMessage"/> </summary>
    internal sealed class RemoteEventMessage
    {
        /** map to <see cref="ProtocolEvent"/> discriminator */ public  string              msg;
        /** map to <see cref="ProtocolEvent.dstClientId"/> */   public  JsonKey             clt;
        /** map to <see cref="EventMessage.events"/> */         public  RemoteSyncEvent[]   events;
    }
    
    /// <summary> Reflect the shape of a <see cref="SyncEvent"/> </summary>
    internal sealed class RemoteSyncEvent
    {
        /** map to <see cref="SyncEvent.seq"/> */               public  int         seq; 
        /** map to <see cref="SyncEvent.srcUserId"/> */         public  JsonKey     src;
        /** map to <see cref="SyncEvent.db"/> */                public  string      db;
        /** map to <see cref="SyncEvent.isOrigin"/> */          public  bool?       isOrigin;
        /** map to <see cref="SyncEvent.tasks"/> */             public  JsonValue[] tasks;
    }
    
    public static class RemoteUtils
    {
        public static JsonValue CreateProtocolMessage (ProtocolMessage message, ObjectPool<ObjectMapper> mapperPool)
        {
            using (var pooledMapper = mapperPool.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                mapper.Pretty = true;
                mapper.WriteNullMembers = false;
                if (EventDispatcher.SerializeRemoteEvents && message is EventMessage eventMessage) {
                    var remoteEventMessage      = new RemoteEventMessage { msg = "ev", clt = eventMessage.dstClientId };
                    var events                  = eventMessage.events;
                    var remoteEvents            = new RemoteSyncEvent[events.Length];
                    remoteEventMessage.events   = remoteEvents;
                    for (int n = 0; n < events.Length; n++) {
                        var ev = events[n];
                        var remoteEv = new RemoteSyncEvent {
                            seq         = ev.seq,
                            src         = ev.srcUserId,
                            db          = ev.db,
                            isOrigin    = ev.isOrigin,
                            tasks       = ev.tasksJson,
                        };
                        remoteEvents[n] = remoteEv;
                    }
                    return mapper.WriteAsValue(remoteEventMessage);
                }
                return mapper.WriteAsValue(message);
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