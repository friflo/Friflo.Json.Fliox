// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public delegate void MessageSubscriptionHandler<TMessage>  (Message<TMessage>  message, EventContext context);
    public delegate void MessageSubscriptionHandler            (Message            message, EventContext context);

    /// <summary>
    /// Expose the <see cref="Name"/> and the <see cref="JsonParam"/> of a received message.
    /// The method <see cref="GetParam{TParam}"/> provide type safe access to the <see cref="JsonParam"/> of a message. 
    /// </summary>
    public interface IMessage {
        /// <summary>Returns the message name.</summary>
        string              Name        { get; }
        /// <summary>
        /// Returns the message param as JSON.
        /// </summary>
        JsonValue           JsonParam   { get; }
        
        /// <summary>
        /// Read the <see cref="JsonParam"/> as the given type <typeparamref name="TParam"/>.
        /// Return false and set <paramref name="error"/> in case <see cref="JsonParam"/> is incompatible to <typeparamref name="TParam"/>
        /// </summary>
        bool                GetParam<TParam>(out TParam   param, out string error);
    } 
    
    /// <summary>
    /// Expose the <see cref="Name"/>, the <see cref="JsonParam"/> and the type safe <see cref="GetParam"/> of a received message.
    /// </summary>
    public readonly struct Message<TParam> : IMessage {
        public              string          Name        { get; }
        public              JsonValue       JsonParam   { get; }
        
        private readonly    ObjectReader    reader;
       
        /// <summary>Return the message <paramref name="param"/></summary> without validation 
        /// <param name="param">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public  bool    GetParam    (out TParam param, out string error) => Message.Read(JsonParam, reader, out param, out error);
        /// <summary>Return the message <paramref name="param"/> as the given type <typeparamref name="T"/> without validation</summary>
        /// <param name="param">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public  bool    GetParam<T> (out T      param, out string error) => Message.Read(JsonParam, reader, out param, out error);

        public override string          ToString()  => $"{Name}(param: {JsonParam.AsString()})";
        
        /// <summary>
        /// <see cref="JsonParam"/> is set to <see cref="SyncMessageTask.param"/> json.
        /// If json is null <see cref="JsonParam"/> is set to "null".
        /// </summary>
        internal Message(in InvokeContext invokeContext) {
            Name        = invokeContext.name;
            JsonParam   = invokeContext.param;  
            reader      = invokeContext.reader;
        }
    }
    
    /// <summary>
    /// Expose the <see cref="Name"/> and the <see cref="JsonParam"/> of a received message.
    /// </summary>
    public readonly struct Message  : IMessage {
        public              string          Name        => invokeContext.name; 
        public              JsonValue       JsonParam   => invokeContext.param;
        
        internal readonly   InvokeContext   invokeContext;
        
        public override     string          ToString()  => $"{Name}(param: {JsonParam.AsString()})";
        
        internal Message(in InvokeContext invokeContext) {
            this.invokeContext = invokeContext;
        }
        
        /// <summary>Return the message <paramref name="param"/></summary> without validation 
        /// <param name="param">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool GetParam<TParam>(out TParam param, out string error) {
            return Read(JsonParam, invokeContext.reader, out param, out error);
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
}
