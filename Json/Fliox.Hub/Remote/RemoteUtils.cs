// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary> Reflect the shape of a <see cref="EventMessage"/> </summary>
    public struct RemoteEventMessage
    {
        /** map to <see cref="ProtocolEvent"/> discriminator */ public  string          msg;
        /** map to <see cref="ProtocolEvent.dstClientId"/> */   public  JsonKey         clt;
        /** map to <see cref="EventMessage.seq"/> */            public  int             seq;
        /** map to <see cref="EventMessage.events"/> */         public  List<JsonValue> events;
    }
    
    /// <summary> Reflect the shape of a <see cref="SyncEvent"/> </summary>
    public struct RemoteSyncEvent
    {
        /** map to <see cref="SyncEvent.srcUserId"/> */         public  JsonKey         src;
        /** map to <see cref="SyncEvent.db"/> */                public  string          db;
        /** map to <see cref="SyncEvent.isOrigin"/> */          public  bool?           isOrigin;
        /** map to <see cref="SyncEvent.tasks"/> */             public  List<JsonValue> tasks;
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
            return new JsonValue(result);
        }
        
        /// <summary>
        /// Creates a serialized <see cref="EventMessage"/><br/>
        /// <b>Attention</b> returned <see cref="JsonValue"/> is <b>only</b> valid until the passed <paramref name="writer"/> is reused
        /// </summary>
        public static JsonValue CreateEventMessage (
            List<JsonValue>     syncEvents,
            in JsonKey          dstClientId,
            int                 seq,
            ObjectWriter        writer)   
        {
            writer.Pretty           = true;
            writer.WriteNullMembers = false;
            var remoteEventMessage  = new RemoteEventMessage { msg = "ev", clt = dstClientId, seq = seq, events = syncEvents };
            var result              = writer.WriteAsBytes(remoteEventMessage);
            return new JsonValue(result);
        }
        
        /// <summary>
        /// <b>Attention</b> returned <see cref="JsonValue"/> is <b>only</b> valid until the passed <paramref name="writer"/> in  is reused
        /// </summary>
        public static JsonValue SerializeSyncEvent (
            in SyncEvent        syncEvent,
            ObjectWriter        writer)       
        {
            writer.Pretty           = true;
            writer.WriteNullMembers = false;
            var remoteEv = new RemoteSyncEvent {
                src         = syncEvent.srcUserId,
                db          = syncEvent.db,
                isOrigin    = syncEvent.isOrigin,
                tasks       = syncEvent.tasksJson
            };
            var result = writer.WriteAsBytes(remoteEv);
            return new JsonValue(result);
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
                // reset intern fields
                if (instancePool != null) {
                    syncRequest.intern = default;
                    foreach (var task in syncRequest.tasks) {
                        task.intern = default;
                    }
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
        
        /// <summary>
        /// Used to parse messages sent to a client
        /// </summary>
        internal static IClientMessage ReadClientMessage (
            in JsonValue    jsonMessage,
            ObjectMapper    mapper,
            out string      error)
        {
            ObjectReader reader = mapper.reader;
            var message         = reader.Read<IClientMessage>(jsonMessage);
            if (reader.Error.ErrSet) {
                error = reader.Error.msg.ToString();
                return null;
            }
            error = null;
            return message;
        }
    }
    
    [Discriminator("msg", "event type")] 
    [PolymorphType(typeof(ClientEventMessage),  "ev")]
    [PolymorphType(typeof(SyncResponse),        "resp")]
    internal interface IClientMessage { }
    
    /// <summary>
    /// Used to identify a <see cref="EventMessage"/> by its discriminator and read its <see cref="ProtocolEvent.dstClientId"/><br/>
    /// Every other member is skipped as the entire JSON message is passed as a <see cref="RemoteEvent.message"/> to an
    /// <see cref="EventReceiver"/>
    /// </summary>
    internal sealed class ClientEventMessage : IClientMessage
    {
        /** map to <see cref="ProtocolEvent.dstClientId"/> */
        [Serialize                    ("clt")]
        [Required]  public  JsonKey     dstClientId = default; // default => avoid warning [CS0649] 'dstClientId' is never assigned
    }
    
    public readonly struct RemoteEvent
    {
        /// <summary>the <see cref="ProtocolEvent.dstClientId"/> of the <see cref="message"/></summary>
        public  readonly    JsonKey     dstClientId;
        /// <summary>serialized <see cref="EventMessage"/></summary>
        public  readonly    JsonValue   message;
        
        public RemoteEvent(in JsonKey dstClientId, in JsonValue message) {
            this.dstClientId    = dstClientId;
            this.message        = message;
        }
    }
}