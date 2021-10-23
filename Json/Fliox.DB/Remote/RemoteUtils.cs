// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Remote
{
    internal class RemoteSubscriptionEvent
    {
        /** map to <see cref="ProtocolEvent"/> Discriminator */ public  string      msg;
        /** map to <see cref="ProtocolEvent.seq"/> */           public  int         seq; 
        /** map to <see cref="ProtocolEvent.srcUserId"/> */     public  JsonKey     src;
        /** map to <see cref="ProtocolEvent.dstClientId"/> */   public  JsonKey     clt;
        /** map to <see cref="EventMessage.tasks"/> */          public  JsonValue[] tasks;
    }
    
    public static class RemoteUtils
    {
        public static JsonValue CreateProtocolMessage (ProtocolMessage message, IPools pools)
        {
            using (var pooledMapper = pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                mapper.Pretty = true;
                mapper.WriteNullMembers = false;
                if (message is EventMessage ev && ev.tasksJson != null) {
                    var remoteEv = new RemoteSubscriptionEvent {
                        msg     = "ev",
                        seq     = ev.seq,
                        src     = ev.srcUserId,
                        clt     = ev.dstClientId,
                        tasks   = ev.tasksJson
                    };
                    return new JsonValue(mapper.WriteAsArray(remoteEv));
                }
                return new JsonValue(mapper.WriteAsArray(message));
            }
        }
        
        public static ProtocolMessage ReadProtocolMessage (JsonValue jsonMessage, IPools pools, out string error)
        {
            using (var pooledMapper = pools.ObjectMapper.Get()) {
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