// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Fliox.DB.Client.Internal;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Client
{
    /// <summary>
    /// A <see cref="MessageTask"/> contains the message (or command) <see cref="name"/> and <see cref="value"/> sent to
    /// an <see cref="EntityDatabase"/>. It is used to send to the message (or command) as en event to all clients which
    /// successful subscribed the message by its <see cref="name"/>.
    /// If the message was sent successful <see cref="SyncTask.Success"/> is true.
    /// <br/>
    /// <b>Note</b>: A message returns no result. To get a result send a command by <see cref="FlioxClient.SendCommand{TCommand,TResult}"/> 
    /// </summary>
    public class MessageTask : SyncTask
    {
        internal readonly   string          name;
        internal readonly   JsonValue       value;
        
        internal            TaskState       state;
        internal override   TaskState       State       => state;
        
        public   override   string          Details     => $"MessageTask (name: {name})";

        
        internal MessageTask(string name, JsonValue value) {
            this.name   = name;
            this.value  = value;
        }
    }

    /// <summary>
    /// Additional to a <see cref="MessageTask"/> a <see cref="CommandTask"/> also provide a command <see cref="ResultJson"/>
    /// after the task is synced successful.
    /// <br/>
    /// <b>Note</b>: For type safe access to the result use <see cref="CommandTask{TResult}"/>
    /// </summary>
    public class CommandTask : MessageTask
    {
        private  readonly   ObjectReader    reader;
        internal            JsonValue       result;

        public   override   string          Details     => $"CommandTask (name: {name})";

        /// <summary>Return the result of a command used as a command as JSON.
        /// JSON is "null" if the command doesnt return a result.
        /// For type safe access of the result use <see cref="ReadResult{T}"/></summary>
        public              JsonValue       ResultJson  => IsOk("CommandTask.ResultJson", out Exception e) ? result : throw e;
        
        internal CommandTask(string name, JsonValue value, ObjectReader reader) : base (name, value) {
            this.reader = reader;
        }

        /// <summary>
        /// Return a type safe result of a command.
        /// The result is null if the command doesnt return a result.
        /// Throws <see cref="JsonReaderException"/> if read fails.
        /// </summary>
        public T ReadResult<T>() {
            var ok = IsOk("CommandTask.ReadResult", out Exception e);
            if (ok) {
                var resultValue = reader.Read<T>(result);
                if (reader.Success)
                    return resultValue;
                var error = reader.Error;
                throw new JsonReaderException (error.msg.AsString(), error.Pos);
            }
            throw e;
        }
        
        /// <summary>
        /// Return a type safe result of a command.
        /// The result is null if the command doesnt return a result.
        /// Return false if read fails and set <see cref="error"/>.
        /// </summary>
        public bool TryReadResult<T>(out T resultValue, out JsonReaderException error) {
            var ok = IsOk("CommandTask.TryReadResult", out Exception e);
            if (ok) {
                resultValue = reader.Read<T>(result);
                if (reader.Success) {
                    error = null;
                    return true;
                }
                var readError = reader.Error;
                error = new JsonReaderException (readError.msg.AsString(), readError.Pos);
                return false;
            }
            throw e;
        }
    }

    /// <summary>
    /// Additional to a <see cref="MessageTask"/> a <see cref="CommandTask{TResult}"/> also provide a type safe access
    /// to the command <see cref="Result"/> after the task is synced successful.
    /// </summary>
    public sealed class CommandTask<TResult> : CommandTask
    {
        public              TResult         Result => ReadResult<TResult>();
        
        internal CommandTask(string name, JsonValue value, ObjectReader reader) : base (name, value, reader) { }
    }
}

