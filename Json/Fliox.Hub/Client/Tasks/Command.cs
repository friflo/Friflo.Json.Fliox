// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// A <see cref="CommandTask"/> contains the command (<b>name</b> and <b>param</b>) send to an <see cref="EntityDatabase"/> using <see cref="FlioxClient.SendCommand{TResult}"/>.
    /// Additional to a <see cref="MessageTask"/> a <see cref="CommandTask"/> also provide a command <see cref="RawResult"/> containing its execution result.
    /// </summary>
    /// <remarks>
    /// <b>Note</b>: For type safe access to the result use <see cref="CommandTask{TResult}"/> returned by
    /// <see cref="FlioxClient.SendCommand{TParam,TResult}"/>
    /// </remarks>
    public class CommandTask : MessageTask
    {
        private  readonly   Pool            pool;
        internal            JsonValue       result;

        public   override   string          Details     => $"CommandTask (name: {name.AsString()})";

        /// <summary>Return the result of a command used as a command as JSON.
        /// JSON is "null" if the command doesn't return a result.
        /// For type safe access of the result use <see cref="ReadResult{T}"/></summary>
        public              JsonValue       RawResult  => IsOk("CommandTask.RawResult", out Exception e) ? result : throw e;
        
        internal CommandTask(in ShortString name, in JsonValue param, Pool pool)
            : base (name, param)
        {
            this.pool = pool;
        }

        /// <summary>
        /// Return a type safe result of a command.
        /// The result is null if the command doesn't return a result.
        /// Throws <see cref="JsonReaderException"/> if read fails.
        /// </summary>
        public T ReadResult<T>() {
            var ok = IsOk("CommandTask.ReadResult", out Exception e);
            if (ok) {
                using (var pooled = pool.ObjectMapper.Get()) {
                    var reader  = pooled.instance.reader;
                    var resultValue = reader.Read<T>(result);
                    if (reader.Success)
                        return resultValue;
                    var error = reader.Error;
                    throw new JsonReaderException (error.msg.AsString(), error.Pos);
                }
            }
            throw e;
        }
        
        /// <summary>
        /// Return a type safe result of a command.
        /// The result is null if the command doesn't return a result.
        /// Return false if read fails and set <paramref name="error"/>.
        /// </summary>
        public bool TryReadResult<T>(out T resultValue, out JsonReaderException error) {
            var ok = IsOk("CommandTask.TryReadResult", out Exception e);
            if (ok) {
                using (var pooled = pool.ObjectMapper.Get()) {
                    var reader  = pooled.instance.reader;
                    resultValue = reader.Read<T>(result);
                    if (reader.Success) {
                        error = null;
                        return true;
                    }
                    var readError = reader.Error;
                    error = new JsonReaderException (readError.msg.AsString(), readError.Pos);
                    return false;
                }
            }
            throw e;
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            var targets = EventTargets;
            return new SendCommand {
                name        = name,
                param       = param,
                intern      = new SyncTaskIntern(this),
                users       = targets?.users,
                clients     = targets?.clients,
                groups      = targets?.groups,
            };
        }
    }

    /// <summary>
    /// A <see cref="CommandTask{TResult}"/> contains the command (<b>name</b> and <b>param</b>) send to an <see cref="EntityDatabase"/> using <see cref="FlioxClient.SendCommand{TResult}"/>.
    /// Its <see cref="Result"/> is available after calling <see cref="FlioxClient.SyncTasks"/>.
    /// </summary>
    /// <remarks>
    /// Additional to a <see cref="MessageTask"/> a <see cref="CommandTask{TResult}"/> also provide type safe access
    /// to the command <see cref="Result"/> after the task is synced successful.
    /// </remarks>
    public sealed class CommandTask<TResult> : CommandTask
    {
        public              TResult         Result => ReadResult<TResult>();
        
        internal CommandTask(in ShortString name, in JsonValue param, Pool pool)
            : base (name, param, pool) { }
    }
}
