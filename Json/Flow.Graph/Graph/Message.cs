// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    public interface IMessage {
        string              Name    { get; }
        string              Json    { get; }
        
        T                   ReadJson   <T>();
        bool                TryReadJson<T>(out T result, out JsonReaderException error);
    } 
    
    public readonly struct Message<TValue> : IMessage {
        public              string          Name    { get; }
        public              string          Json    { get; }
        
        private readonly    ObjectReader    reader;
       
        public              TValue          Value => Message.Read<TValue>(Json, reader);

        public override string              ToString()  => Name;
        
        /// <summary>
        /// <see cref="Json"/> is set to <see cref="SendMessage.value"/> json.
        /// If json is null <see cref="Json"/> is set to "null".
        /// </summary>
        public Message(string name, string json, ObjectReader reader) {
            Name        = name;
            Json        = json ?? "null";  
            this.reader = reader;
        }
        
        public T ReadJson<T>() {
            return Message.Read<T>(Json, reader);
        }
        
        public bool TryReadJson<T>(out T result, out JsonReaderException error) {
            return Message.TryRead(Json, reader, out result, out error);
        }
    }
    
    public readonly struct Message  : IMessage {
        public              string          Name    { get; }
        public              string          Json    { get; }
        
        private readonly    ObjectReader    reader;
        
        public override string              ToString()  => Name;
        
        public Message(string name, string json, ObjectReader reader) {
            Name        = name;
            Json        = json ?? "null";
            this.reader = reader;
        }
        
        public T ReadJson<T>() {
            return Read<T>(Json, reader);
        }
        
        public bool TryReadJson<T>(out T result, out JsonReaderException error) {
            return TryRead(Json, reader, out result, out error);
        }
        
        // --- internals
        internal static T Read<T>(string json, ObjectReader reader) {
            var result = reader.Read<T>(json);
            if (reader.Success)
                return result;
            var error = reader.Error;
            throw new JsonReaderException (error.msg.ToString(), error.Pos);
        }
        
        internal static bool TryRead<T>(string json, ObjectReader reader, out T result, out JsonReaderException error) {
            result = reader.Read<T>(json);
            if (reader.Success) {
                error = null;
                return true;
            }
            var readError = reader.Error;
            error = new JsonReaderException (readError.msg.ToString(), readError.Pos);
            return false;
        }
    }

    public delegate void Handler<TValue>(Message<TValue> msg);
    public delegate void Handler        (Message         msg);
}
