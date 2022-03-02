// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Expose the <see cref="Name"/> and the <see cref="JsonParam"/> value of a received message.
    /// The method <see cref="GetParam{TParam}"/> provide type safe access to the <see cref="JsonParam"/> value of a message. 
    /// </summary>
    public interface IMessage {
        /// <summary>Returns the message name.</summary>
        string              Name        { get; }
        /// <summary>
        /// Returns the message value as JSON.
        /// </summary>
        JsonValue           JsonParam   { get; }
        
        /// <summary>
        /// Read the <see cref="JsonParam"/> value as the given type <typeparamref name="TParam"/>.
        /// Return false and set <paramref name="error"/> in case <see cref="JsonParam"/> is incompatible to <typeparamref name="TParam"/>
        /// </summary>
        bool                GetParam<TParam>(out TParam   param, out string error);
    } 
    
    /// <summary>
    /// Expose the <see cref="Name"/>, the <see cref="JsonParam"/> value and the type safe <see cref="GetParam"/> of a received message.
    /// </summary>
    public readonly struct Message<TMessage> : IMessage {
        public              string          Name        { get; }
        public              JsonValue       JsonParam   { get; }
        
        private readonly    ObjectReader    reader;
       
        /// <summary>
        /// Return the <see cref="JsonParam"/> value as the specified type <typeparamref name="TMessage"/>.
        /// </summary>
        public  bool    GetParam        (out TMessage param, out string error) => Message.Read(JsonParam, reader, out param, out error);
        public  bool    GetParam<TParam>(out TParam   param, out string error) => Message.Read(JsonParam, reader, out param, out error);

        public override string          ToString()  => Name;
        
        /// <summary>
        /// <see cref="JsonParam"/> is set to <see cref="SyncMessageTask.param"/> json.
        /// If json is null <see cref="JsonParam"/> is set to "null".
        /// </summary>
        internal Message(string name, in JsonValue param, ObjectReader reader) {
            Name        = name;
            JsonParam   = param;  
            this.reader = reader;
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
        
        internal Message(string name, in JsonValue param, ObjectReader reader) {
            Name        = name;
            JsonParam   = param;
            this.reader = reader;
        }
        
        public bool GetParam<TParam>(out TParam param, out string error) {
            return Read(JsonParam, reader, out param, out error);
        }
        
        // --- internals
        internal static bool Read<T>(in JsonValue json, ObjectReader reader, out T result, out string error) {
            result = reader.Read<T>(json);
            if (reader.Success) {
                error = null;
                return true;
            }
            error = reader.Error.msg.AsString();
            return false;
        }
    }
    
    public delegate void MessageHandler<TMessage>   (Message<TMessage>  message);
    public delegate void MessageHandler             (Message            message);
}
