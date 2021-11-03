// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Remote
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
        public static JsonValue CreateProtocolMessage (ProtocolMessage message, IPool pool)
        {
            using (var pooledMapper = pool.ObjectMapper.Get()) {
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
        
        public static ProtocolMessage ReadProtocolMessage (JsonValue jsonMessage, IPool pool, out string error)
        {
            using (var pooledMapper = pool.ObjectMapper.Get()) {
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