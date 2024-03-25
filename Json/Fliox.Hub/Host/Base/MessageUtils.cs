// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary> Reflect the shape of a <see cref="EventMessage"/> </summary>
    public struct RemoteEventMessage
    {
        /** map to <see cref="ProtocolEvent"/> discriminator */ public  string          msg;
        /** map to <see cref="ProtocolEvent.dstClientId"/> */   public  ShortString     clt;
        /** map to <see cref="EventMessage.seq"/> */            public  int             seq;
        /** map to <see cref="EventMessage.events"/> */         public  List<JsonValue> ev;
    }
    
    /// <summary> Reflect the shape of a <see cref="SyncEvent"/> </summary>
    public struct RemoteSyncEvent
    {
        /** map to <see cref="SyncEvent.usr"/> */               public  ShortString     usr;
        /** map to <see cref="SyncEvent.db"/> */                public  ShortString     db;
        /** map to <see cref="SyncEvent.clt"/> */               public  ShortString     clt;
        /** map to <see cref="SyncEvent.tasks"/> */             public  List<JsonValue> tasks;
    }
    
    /// <summary> Reflect the shape of a <see cref="SyncMessageTask"/> </summary>
    public struct RemoteMessageTask
    {
        /** map to <see cref="SyncMessageTask"/> discriminator*/public  string          task;
        /** map to <see cref="SyncMessageTask.name"/> */        public  ShortString     name;
        /** map to <see cref="SyncMessageTask.param"/> */       public  JsonValue       param;
    }
    
    /// <summary>
    /// <b>Attention</b> all <c>Create</c> methods return a <see cref="JsonValue"/> which are only valid until the
    /// passed <see cref="ObjectReader"/> it reused 
    /// </summary>
    public static class MessageUtils
    {
        public static ObjectWriter GetCompactWriter (ObjectMapper mapper) {
            var writer              = mapper.writer;
            writer.Pretty           = false;
            writer.WriteNullMembers = false;
            return writer;
        }
        
        public static ObjectWriter GetPrettyWriter (ObjectMapper mapper) {
            var writer              = mapper.writer;
            writer.Pretty           = true;
            writer.WriteNullMembers = false;
            return writer;
        }
        
        /// <summary>
        /// <b>Attention</b> returned <see cref="JsonValue"/> is <b>only</b> valid until the passed <paramref name="writer"/> is reused
        /// </summary>
        public static JsonValue WriteProtocolMessage (
            ProtocolMessage message,
            SharedEnv       env,
            ObjectWriter    writer)
        {
            var result  = writer.WriteAsBytesMapper(message, env.types.protocol);
            return new JsonValue(result);
        }
        
        /// <summary>
        /// Creates a serialized <see cref="EventMessage"/><br/>
        /// <b>Attention</b> returned <see cref="JsonValue"/> is <b>only</b> valid until the passed <paramref name="writer"/> is reused
        /// </summary>
        public static JsonValue WriteEventMessage (
            List<JsonValue>     syncEvents,
            in ShortString      dstClientId,
            int                 seq,
            ObjectWriter        writer)
        {
            var remoteEventMessage  = new RemoteEventMessage { msg = "ev", clt = dstClientId, seq = seq, ev = syncEvents };
            var result              = writer.WriteAsBytes(remoteEventMessage);
            return new JsonValue(result);
        }
        
        /// <summary>
        /// <b>Attention</b> returned <see cref="JsonValue"/> is <b>only</b> valid until the passed <paramref name="writer"/> in  is reused
        /// </summary>
        internal static JsonValue WriteSyncTask (SyncRequestTask task, ObjectWriter writer) {
            if (task is SyncMessageTask message) {
                // Note: Serialize only message name & param. Not users, clients & groups.
                // This avoids leaking information of other network participants to subscribers.
                var discriminant  = message.TaskType == TaskType.message ? "msg" : "cmd";
                var remoteMessage = new RemoteMessageTask { task = discriminant, name = message.name, param = message.param };
                return new JsonValue(writer.WriteAsBytes(remoteMessage));
            }
            return new JsonValue(writer.WriteAsBytes(task));
        }
        
        /// <summary>
        /// <b>Attention</b> returned <see cref="JsonValue"/> is <b>only</b> valid until the passed <paramref name="writer"/> in  is reused
        /// </summary>
        public static JsonValue WriteSyncEvent (
            in SyncEvent        syncEvent,
            ObjectWriter        writer)       
        {
            var remoteEv = new RemoteSyncEvent {
                usr         = syncEvent.usr,
                db          = syncEvent.db,
                clt         = syncEvent.clt,
                tasks       = syncEvent.tasksJson
            };
            var result = writer.WriteAsBytes(remoteEv);
            return new JsonValue(result);
        }
        
        public static SyncRequest ReadSyncRequest (
            ObjectReader    reader,
            SharedEnv       env,
            in JsonValue    jsonMessage,
            out string      error)
        {
            var message = reader.ReadMapper(env.types.protocol, jsonMessage);
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
        
        public static ProtocolMessage ReadProtocolMessage (
            in JsonValue    jsonMessage,
            SharedEnv       env,
            ObjectReader    reader,
            out string      error)
        {
            var message = reader.ReadMapper(env.types.protocol, jsonMessage);
            if (reader.Error.ErrSet) {
                error = reader.Error.msg.ToString();
                return null;
            }
            error = null;
            return message;
        }
        
        /// <summary>
        /// Parse the header of the given <paramref name="rawMessage"/> object.<br/>
        /// The returned <see cref="MessageHead.type"/> enables the caller further processing.<br/>
        /// Parsing stops when the required members are read.
        /// </summary>
        internal static MessageHead ReadMessageHead(ref Utf8JsonParser parser, in JsonValue rawMessage)
        {
            parser.InitParser(rawMessage);
            var ev = parser.NextEvent();
            if (ev != JsonEvent.ObjectStart) {
                return default;
            }
            MessageHead result = default;
            
            while (true) {
                ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNull:
                    case JsonEvent.ValueBool:
                        continue;
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                        return result; // return as objects and arrays are not a header anymore
                    case JsonEvent.ObjectEnd:
                    case JsonEvent.ArrayEnd:
                        throw new InvalidOperationException("unexpected state");
                    case JsonEvent.ValueString:
                        if (parser.key.IsEqual(msg)) {
                            var key     = new BytesHash (parser.value);
                            MessageTypes.TryGetValue(key, out result.type);
                            continue;
                        }
                        if (parser.key.IsEqual(clt)) {
                            result.dstClientId  = new ShortString(parser.value, null);
                            return result;
                        }
                        continue;
                    case JsonEvent.ValueNumber:
                        if (parser.key.IsEqual(req)) {
                            result.reqId        = parser.ValueAsInt(out bool _);
                            return result;
                        }
                        if (parser.key.IsEqual(clt)) {
                            result.dstClientId  = new ShortString(parser.value, null);
                            return result;
                        }
                        continue;
                    case JsonEvent.Error:
                        return default;
                }
            }
        }
        
        private static Dictionary<BytesHash, MessageType> CreateMessageTypes() {
            var map = new Dictionary<BytesHash, MessageType>(BytesHash.Equality) {
                { new BytesHash(new Bytes("ev")),       MessageType.ev   },
                { new BytesHash(new Bytes("sync")),     MessageType.sync },
                { new BytesHash(new Bytes("resp")),     MessageType.resp },
                { new BytesHash(new Bytes("error")),    MessageType.error }
            };
            return map;
        }
        
        private static readonly Dictionary<BytesHash, MessageType>  MessageTypes = CreateMessageTypes();  
        
        // ReSharper disable InconsistentNaming
        private static readonly     Bytes   msg         = new Bytes("msg");
        private static readonly     Bytes   clt         = new Bytes("clt");
        private static readonly     Bytes   req         = new Bytes("req");
    }
    
    internal struct MessageHead
    {
        internal    MessageType type;
        internal    ShortString dstClientId;
        internal    int?        reqId;
    } 
}