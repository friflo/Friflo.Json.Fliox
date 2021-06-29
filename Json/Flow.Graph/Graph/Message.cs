// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Graph
{
    public interface IMessage {
        string              Name    { get; }
        string              Json    { get; }
        ObjectReader        Reader  { get; }
    } 
    
    public readonly struct Message<TValue> : IMessage {
        public          string          Name    { get; }
        public          string          Json    { get; }
        public          ObjectReader    Reader  { get; }
       
        public          TValue          Value       => Reader.Read<TValue>(Json);
        public override string          ToString()  => Name;

        public Message(string name, string json, ObjectReader reader) {
            Name    = name;
            Json    = json;
            Reader  = reader;
        }
    }
    
    public readonly struct Message  : IMessage {
        public          string          Name    { get; }
        public          string          Json    { get; }
        public          ObjectReader    Reader  { get; }
        public override string          ToString()  => Name;
        
        public Message(string name, string json, ObjectReader reader) {
            Name    = name;
            Json    = json;
            Reader  = reader;
        }
    }

    public delegate void Handler<TValue>(Message<TValue> msg);
    public delegate void Handler        (Message         msg);
}
