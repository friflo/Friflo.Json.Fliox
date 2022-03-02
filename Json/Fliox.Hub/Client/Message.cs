// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Expose the <see cref="Name"/> and the <see cref="JsonParam"/> value of a received message.
    /// The methods <see cref="ReadJson{T}"/> and <see cref="TryReadJson{T}"/> provide type safe access to
    /// the <see cref="JsonParam"/> value of a message. 
    /// </summary>
    public interface IMessage {
        /// <summary>Returns the message name.</summary>
        string              Name        { get; }
        /// <summary>
        /// Returns the message value as JSON.
        /// </summary>
        JsonValue           JsonParam   { get; }
        
        /// <summary>
        /// Read the <see cref="JsonParam"/> value as the given type <typeparamref name="T"/>.
        /// Throws a <see cref="JsonReaderException"/> in case <see cref="JsonParam"/> is incompatible to <typeparamref name="T"/></summary>
        T                   ReadJson   <T>();
        
        /// <summary>
        /// Read the <see cref="JsonParam"/> value as the given type <typeparamref name="T"/>.
        /// Return false and set <paramref name="error"/> in case <see cref="JsonParam"/> is incompatible to <typeparamref name="T"/>
        /// </summary>
        bool                TryReadJson<T>(out T result, out JsonReaderException error);
    } 
    
    /// <summary>
    /// Expose the <see cref="Name"/>, the <see cref="JsonParam"/> value and the type safe <see cref="Value"/> of a received message.
    /// </summary>
    public readonly struct Message<TMessage> : IMessage {
        public              string          Name        { get; }
        public              JsonValue       JsonParam   { get; }
        
        private readonly    ObjectReader    reader;
       
        /// <summary>
        /// Return the <see cref="JsonParam"/> value as the specified type <typeparamref name="TMessage"/>.
        /// </summary>
        public              TMessage        Value => Message.Read<TMessage>(JsonParam, reader);

        public override     string          ToString()  => Name;
        
        /// <summary>
        /// <see cref="JsonParam"/> is set to <see cref="SyncMessageTask.param"/> json.
        /// If json is null <see cref="JsonParam"/> is set to "null".
        /// </summary>
        internal Message(string name, JsonValue param, ObjectReader reader) {
            Name        = name;
            JsonParam   = param;  
            this.reader = reader;
        }
        
        public T ReadJson<T>() {
            return Message.Read<T>(JsonParam, reader);
        }
        
        public bool TryReadJson<T>(out T result, out JsonReaderException error) {
            return Message.TryRead(JsonParam, reader, out result, out error);
        }
    }
    
    /// <summary>
    /// Expose the <see cref="Name"/> and the <see cref="JsonParam"/> value of a received message.
    /// </summary>
    public readonly struct Message  : IMessage {
        public              string          Name        { get; }
        public              JsonValue       JsonParam   { get; }
        
        private readonly    ObjectReader    reader;
        
        public override     string          ToString()  => Name;
        
        internal Message(string name, JsonValue param, ObjectReader reader) {
            Name        = name;
            JsonParam   = param;
            this.reader = reader;
        }
        
        public T ReadJson<T>() {
            return Read<T>(JsonParam, reader);
        }
        
        public bool TryReadJson<T>(out T result, out JsonReaderException error) {
            return TryRead(JsonParam, reader, out result, out error);
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
    
    public delegate void MessageHandler<TMessage>   (Message<TMessage>  message);
    public delegate void MessageHandler             (Message            message);
}
