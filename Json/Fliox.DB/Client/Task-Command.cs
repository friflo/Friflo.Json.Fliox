// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Fliox.DB.Client.Internal;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Client
{
    public class CommandTask : SyncTask
    {
        internal readonly   string          name;
        internal readonly   JsonValue       value;
        private  readonly   ObjectReader    reader;
        
        internal            JsonValue       result;
        
        internal            TaskState       state;
        internal override   TaskState       State       => state;
        
        public   override   string          Details     => $"CommandTask (name: {name})";

        /// <summary>Return the result of a command used as a command as JSON.
        /// JSON is "null" if the command doesnt return a result.
        /// For type safe access of the result use <see cref="ReadResult{T}"/></summary>
        public              JsonValue       ResultJson  => IsOk("CommandTask.ResultJson", out Exception e) ? result : throw e;
        
        internal CommandTask(string name, JsonValue value, ObjectReader reader) {
            this.name   = name;
            this.value  = value;
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
    
    
    public sealed class CommandTask<TResult> : CommandTask
    {
        public              TResult          Result => ReadResult<TResult>();
        
        internal CommandTask(string name, JsonValue value, ObjectReader reader) : base (name, value, reader) {
        }
    }
}

