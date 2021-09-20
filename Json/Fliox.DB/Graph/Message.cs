// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph
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
        /// Returns "null" when message was sent by <see cref="EntityStore.SendMessage"/>
        /// </summary>
        JsonUtf8           Json    { get; }
        
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
    public readonly struct Message<TValue> : IMessage {
        public              string          Name    { get; }
        public              JsonUtf8        Json    { get; }
        
        private readonly    ObjectReader    reader;
       
        /// <summary>
        /// Return the <see cref="Json"/> value as the specified type <see cref="TValue"/>.
        /// </summary>
        public              TValue          Value => Message.Read<TValue>(Json, reader);

        public override     string          ToString()  => Name;
        
        /// <summary>
        /// <see cref="Json"/> is set to <see cref="SendMessage.value"/> json.
        /// If json is null <see cref="Json"/> is set to "null".
        /// </summary>
        internal Message(string name, JsonUtf8 json, ObjectReader reader) {
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
        public              JsonUtf8        Json    { get; }
        
        private readonly    ObjectReader    reader;
        
        public override     string          ToString()  => Name;
        
        internal Message(string name, JsonUtf8 json, ObjectReader reader) {
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
        internal static T Read<T>(JsonUtf8 json, ObjectReader reader) {
            var result = reader.Read<T>(json);
            if (reader.Success)
                return result;
            var error = reader.Error;
            throw new JsonReaderException (error.msg.AsString(), error.Pos);
        }
        
        internal static bool TryRead<T>(JsonUtf8 json, ObjectReader reader, out T result, out JsonReaderException error) {
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
    
    public static class StdMessage  {
        /// <summary>
        /// Echoes the value specified in <see cref="EntityStore.SendMessage{T}(string, T)"/> in <see cref="SendMessageTask.ResultJson"/>
        /// </summary>
        public const string Echo = "Echo";
    }

    public delegate void MessageHandler<TValue>(Message<TValue> msg);
    public delegate void MessageHandler        (Message         msg);
}
