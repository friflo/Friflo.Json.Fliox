// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;

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
    
    /// <summary>
    /// <b>Attention</b> all <c>Create</c> methods return a <see cref="JsonValue"/> which are only valid until the
    /// passed <see cref="ObjectMapper"/> it reused 
    /// </summary>
    public static class RemoteUtils
    {
        /// <summary>
        /// <b>Attention</b> returned <see cref="JsonValue"/> is <b>only</b> valid until the passed <paramref name="mapper"/> is reused
        /// </summary>
        public static JsonValue CreateProtocolMessage (
            ProtocolMessage message,
            ObjectMapper    mapper)
        {
            mapper.Pretty           = true;
            mapper.WriteNullMembers = false;
            var result              = mapper.writer.WriteAsBytes(message);
            return new JsonValue(ref result);
        }
        
        /// <summary>
        /// <b>Attention</b> returned <see cref="JsonValue"/> is <b>only</b> valid until the passed <paramref name="args"/> mapper in  is reused
        /// </summary>
        public static JsonValue CreateProtocolEvent (
            EventMessage        eventMessage,
            in SendEventArgs    args)
        {
            var mapper              = args.mapper;
            mapper.Pretty           = true;
            mapper.WriteNullMembers = false;
            if (!EventDispatcher.SerializeRemoteEvents) {
                var ev = mapper.writer.WriteAsBytes(eventMessage);
                return new JsonValue(ref ev);
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
            var result = mapper.writer.WriteAsBytes(remoteEventMessage);
            return new JsonValue(ref result);
        }
        
        public static SyncRequest ReadSyncRequest (
            ObjectMapper    mapper,
            in JsonValue    jsonMessage,
            out string      error)
        {
            var reader  = mapper.reader;
            var message = reader.Read<ProtocolMessage>(jsonMessage);
            if (reader.Error.ErrSet) {
                error = reader.Error.GetMessage();
                return null;
            }
            if (message is SyncRequest syncRequest) {
                var instancePool = reader.InstancePool;
                // reset cached task.json created by SerializeRemoteEvent()
                if (instancePool != null) {
                    foreach (var task in syncRequest.tasks) { task.json = null; }
                }
                error = null;
                return syncRequest;
            }
            error = $"Expect 'sync' request. was: '{message.MessageType}'";
            return null;
        }
        
        public static ProtocolMessage ReadProtocolMessage (
            in JsonValue    jsonMessage,
            ObjectMapper    mapper,
            out string      error)
        {
            ObjectReader reader = mapper.reader;
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