// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Expose the <see cref="Name"/> and the <see cref="Json"/> value of a received message.
    /// The methods <see cref="ReadJson{T}"/> and <see cref="TryReadJson{T}"/> provide type safe access to
    /// the <see cref="Json"/> value of a message. 
    /// </summary>
    public interface IMessage {
        /// <summary>Returns the message name.</summary>
        string              Name    { get; }
        /// <summary>
        /// Returns the message value as JSON.
        /// </summary>
        JsonValue           Json    { get; }
        
        /// <summary>
        /// Read the <see cref="Json"/> value as the given type <see cref="T"/>.
        /// Throws a <see cref="JsonReaderException"/> in case <see cref="Json"/> is incompatible to <see cref="T"/></summary>
        T                   ReadJson   <T>();
        
        /// <summary>
        /// Read the <see cref="Json"/> value as the given type <see cref="T"/>.
        /// Return false and set <see cref="error"/> in case <see cref="Json"/> is incompatible to <see cref="T"/>
        /// </summary>
        bool                TryReadJson<T>(out T result, out JsonReaderException error);
    } 
    
    /// <summary>
    /// Expose the <see cref="Name"/>, the <see cref="Json"/> value and the type safe <see cref="Value"/> of a received message.
    /// </summary>
    public readonly struct Message<TMessage> : IMessage {
        public              string          Name    { get; }
        public              JsonValue       Json    { get; }
        
        private readonly    ObjectReader    reader;
       
        /// <summary>
        /// Return the <see cref="Json"/> value as the specified type <see cref="TMessage"/>.
        /// </summary>
        public              TMessage        Value => Message.Read<TMessage>(Json, reader);

        public override     string          ToString()  => Name;
        
        /// <summary>
        /// <see cref="Json"/> is set to <see cref="SendCommand.value"/> json.
        /// If json is null <see cref="Json"/> is set to "null".
        /// </summary>
        internal Message(string name, JsonValue json, ObjectReader reader) {
            Name        = name;
            Json        = json;  
            this.reader = reader;
        }
        
        public T ReadJson<T>() {
            return Message.Read<T>(Json, reader);
        }
        
        public bool TryReadJson<T>(out T result, out JsonReaderException error) {
            return Message.TryRead(Json, reader, out result, out error);
        }
    }
    
    /// <summary>
    /// Expose the <see cref="Name"/> and the <see cref="Json"/> value of a received message.
    /// </summary>
    public readonly struct Message  : IMessage {
        public              string          Name    { get; }
        public              JsonValue       Json    { get; }
        
        private readonly    ObjectReader    reader;
        
        public override     string          ToString()  => Name;
        
        internal Message(string name, JsonValue json, ObjectReader reader) {
            Name        = name;
            Json        = json;
            this.reader = reader;
        }
        
        public T ReadJson<T>() {
            return Read<T>(Json, reader);
        }
        
        public bool TryReadJson<T>(out T result, out JsonReaderException error) {
            return TryRead(Json, reader, out result, out error);
        }
        
        // --- internals
        internal static T Read<T>(JsonValue json, ObjectReader reader) {
            var result = reader.Read<T>(json);
            if (reader.Success)
                return result;
            var error = reader.Error;
            throw new JsonReaderException (error.msg.AsString(), error.Pos);
        }
        
        internal static bool TryRead<T>(JsonValue json, ObjectReader reader, out T result, out JsonReaderException error) {
            result = reader.Read<T>(json);
            if (reader.Success) {
                error = null;
                return true;
            }
            var readError = reader.Error;
            error = new JsonReaderException (readError.msg.AsString(), readError.Pos);
            return false;
        }
    }
    
    public static class StdCommand  {
        // --- Db*
        /// <summary>
        /// Echoes the value specified in <see cref="FlioxClient.SendCommand{TParam,TResult}(string,TParam)"/> in <see cref="CommandTask.ResultJson"/>
        /// </summary>
        public const string DbEcho          = "DbEcho";        
        
        public const string DbContainers    = "DbContainers";

        public const string DbCommands      = "DbCommands";

        public const string DbSchema        = "DbSchema";
        
        // --- Hub*
        public const string HubInfo         = "HubInfo";

        public const string HubCluster      = "HubCluster";
    }

    public delegate void MessageHandler<TMessage>   (Message<TMessage>  message);
    public delegate void MessageHandler             (Message            message);
}
