// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Defines signature of the handler method passed to <see cref="FlioxClient.SubscribeMessage"/>
    /// </summary>
    /// <seealso cref="SubscriptionEventHandler"/>
    public delegate void MessageSubscriptionHandler            (Message            message, EventContext context);
    /// <summary>
    /// Defines signature of the handler method passed to <see cref="FlioxClient.SubscribeMessage{TMessage}"/>
    /// </summary>
    /// <seealso cref="SubscriptionEventHandler"/>
    public delegate void MessageSubscriptionHandler<TMessage>  (Message<TMessage>  message, EventContext context);

    /// <summary>
    /// Expose the <see cref="Name"/> and the <see cref="RawParam"/> of a received message.
    /// Use <see cref="GetParam{TParam}"/> to get type safe access to the <see cref="RawParam"/> of a message. 
    /// </summary>
    public interface IMessage {
        /// <summary>message name</summary>
        string              Name        { get; }
        /// <summary>raw message parameter as JSON</summary>
        JsonValue           RawParam    { get; }
        
        /// <summary>
        /// Read the <see cref="RawParam"/> as the given type <typeparamref name="TParam"/>.
        /// Return false and set <paramref name="error"/> in case <see cref="RawParam"/> is incompatible to <typeparamref name="TParam"/>
        /// </summary>
        bool                GetParam<TParam>(out TParam   param, out string error);
    } 
    
    /// <summary>
    /// Expose the <see cref="Name"/>, the <see cref="RawParam"/> and the type safe <see cref="GetParam"/> of a received message.
    /// </summary>
    public readonly struct Message<TParam> : IMessage {
        /// <summary>message name</summary>
        public              string          Name       => invokeContext.name.AsString();
        /// <summary>raw message parameter as JSON</summary>
        public              JsonValue       RawParam   => invokeContext.param;
        
        private             TParam          DebugParam {
            get { Message.Read(invokeContext.param, invokeContext.reader, out TParam param, out _); return param; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly    InvokeContext   invokeContext;
       
        /// <summary>Return the message <paramref name="param"/></summary> without validation 
        /// <param name="param">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public  bool    GetParam    (out TParam param, out string error) => Message.Read(invokeContext.param, invokeContext.reader, out param, out error);
        /// <summary>Return the message <paramref name="param"/> as the given type <typeparamref name="T"/> without validation</summary>
        /// <param name="param">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public  bool    GetParam<T> (out T      param, out string error) => Message.Read(invokeContext.param, invokeContext.reader, out param, out error);

        public override string          ToString()  => $"{Name} (param: {invokeContext.param.AsString()})";
        
        /// <summary>
        /// <see cref="RawParam"/> is set to <see cref="SyncMessageTask.param"/> json.
        /// If json is null <see cref="RawParam"/> is set to "null".
        /// </summary>
        internal Message(in InvokeContext invokeContext) {
            this.invokeContext = invokeContext;
        }
    }
    
    /// <summary>
    /// Expose the <see cref="Name"/> and the <see cref="RawParam"/> of a received message.
    /// </summary>
    public readonly struct Message  : IMessage {
        /// <summary>message name</summary>
        public              string          Name        => invokeContext.name.AsString();
        /// <summary>raw message parameter as JSON</summary>
        public              JsonValue       RawParam    => invokeContext.param;
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal readonly   InvokeContext   invokeContext;
        
        public override     string          ToString()  => $"{Name} (param: {invokeContext.param.AsString()})";
        
        internal Message(in InvokeContext invokeContext) {
            this.invokeContext = invokeContext;
        }
        
        /// <summary>Return the message <paramref name="param"/></summary> without validation 
        /// <param name="param">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool GetParam<TParam>(out TParam param, out string error) {
            return Read(invokeContext.param, invokeContext.reader, out param, out error);
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
