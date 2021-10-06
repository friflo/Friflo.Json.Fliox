// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Remote
{
    internal class RemoteSubscriptionEvent
    {
        /** map to <see cref="ProtocolEvent"/> Discriminator */ public  string      type;
        /** map to <see cref="ProtocolEvent.seq"/> */           public  int         seq; 
        /** map to <see cref="ProtocolEvent.srcUserId"/> */     public  JsonKey     src;
        /** map to <see cref="ProtocolEvent.dstClientId"/> */   public  JsonKey     clt;
        /** map to <see cref="SubscriptionEvent.tasks"/> */     public  JsonValue[] tasks;
    }
    
    public static class RemoteUtils
    {
        public static JsonUtf8 CreateProtocolMessage (ProtocolMessage message, IPools pools)
        {
            using (var pooledMapper = pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                mapper.Pretty = true;
                mapper.WriteNullMembers = false;
                if (message is SubscriptionEvent sub && sub.tasksJson != null) {
                    var remoteEv = new RemoteSubscriptionEvent {
                        type    = "sub",
                        seq     = sub.seq,
                        src     = sub.srcUserId,
                        clt     = sub.dstClientId,
                        tasks   = sub.tasksJson
                    };
                    return new JsonUtf8(mapper.WriteAsArray(remoteEv));
                }
                return new JsonUtf8(mapper.WriteAsArray(message));
            }
        }
        
        public static ProtocolMessage ReadProtocolMessage (JsonUtf8 jsonMessage, IPools pools, out string error)
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