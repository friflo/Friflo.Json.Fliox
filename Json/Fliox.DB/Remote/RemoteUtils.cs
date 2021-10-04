// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Remote
{
    public static class RemoteUtils
    {
        public static JsonUtf8 CreateProtocolMessage (ProtocolMessage message, IPools pools)
        {
            using (var pooledMapper = pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                mapper.Pretty = true;
                mapper.WriteNullMembers = false;
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