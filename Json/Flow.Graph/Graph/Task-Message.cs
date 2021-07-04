// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Graph
{
    public class SendMessageTask : SyncTask
    {
        internal readonly   string          name;
        internal readonly   JsonValue       value;
        private  readonly   ObjectReader    reader;
        
        internal            string          result;
        
        internal            TaskState       state;
        internal override   TaskState       State       => state;
        
        public   override   string          Details     => $"SendMessageTask (name: {name})";

        /// <summary>Return the result of a message used as a command as JSON.
        /// JSON is "null" if the message doesnt return a result.
        /// For type safe access of the result use <see cref="ReadResult{T}"/></summary>
        public              string          ResultJson  => IsOk("SendMessageTask.Result", out Exception e) ? result : throw e;
        
        internal SendMessageTask(string name, string value, ObjectReader reader) {
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
                var resultValue = reader.Read<T>(result);
                if (reader.Success)
                    return resultValue;
                var error = reader.Error;
                throw new JsonReaderException (error.msg.ToString(), error.Pos);
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
                resultValue = reader.Read<T>(result);
                if (reader.Success) {
                    error = null;
                    return true;
                }
                var readError = reader.Error;
                error = new JsonReaderException (readError.msg.ToString(), readError.Pos);
                return false;
            }
            throw e;
        }
    }
    
    /*
    // Could be an alternative solution to get a type safe Result.
    // But using it is cumbersome as it requires to specify Request & Response types as generic parameter.
    public class SendMessageTask<TResult> : SendMessageTask
    {
        public              TResult          Result2 => GetResult<TResult>();
        
        internal SendMessageTask(string name, string value, ObjectReader reader) : base (name, value, reader) {
        }
    } */
}

