// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Fliox.DB.Graph.Internal;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph
{
    public class SendMessageTask : SyncTask
    {
        internal readonly   string          name;
        internal readonly   JsonValue       value;
        private  readonly   ObjectReader    reader;
        
        internal            Utf8Array       result;
        
        internal            TaskState       state;
        internal override   TaskState       State       => state;
        
        public   override   string          Details     => $"SendMessageTask (name: {name})";

        /// <summary>Return the result of a message used as a command as JSON.
        /// JSON is "null" if the message doesnt return a result.
        /// For type safe access of the result use <see cref="ReadResult{T}"/></summary>
        public              Utf8Array       ResultJson  => IsOk("SendMessageTask.Result", out Exception e) ? result : throw e;
        
        internal SendMessageTask(string name, Utf8Array value, ObjectReader reader) {
            this.name   = name;
            this.value  = new JsonValue { json = value };
            this.reader = reader;
        }

        /// <summary>
        /// Return a type safe result of a message (message used as a command).
        /// The result is null if the message doesnt return a result.
        /// Throws <see cref="JsonReaderException"/> if read fails.
        /// </summary>
        public T ReadResult<T>() {
            var ok = IsOk("SendMessageTask.Result", out Exception e);
            if (ok) {
                var resultValue = reader.Read<T>(result.array);
                if (reader.Success)
                    return resultValue;
                var error = reader.Error;
                throw new JsonReaderException (error.msg.AsString(), error.Pos);
            }
            throw e;
        }
        
        /// <summary>
        /// Return a type safe result of a message (message used as a command).
        /// The result is null if the message doesnt return a result.
        /// Return false if read fails and set <see cref="error"/>.
        /// </summary>
        public bool TryReadResult<T>(out T resultValue, out JsonReaderException error) {
            var ok = IsOk("SendMessageTask.Result", out Exception e);
            if (ok) {
                resultValue = reader.Read<T>(result.array);
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
    
    
    public class SendMessageTask<TResult> : SendMessageTask
    {
        public              TResult          Result => ReadResult<TResult>();
        
        internal SendMessageTask(string name, Utf8Array value, ObjectReader reader) : base (name, value, reader) {
        }
    }
}

