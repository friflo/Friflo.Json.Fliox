// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// A <see cref="MessageTask"/> contains the message / command (<b>name</b> and <b>param</b>) sent to
    /// an <see cref="EntityDatabase"/> by <see cref="FlioxClient.SendMessage"/>
    /// </summary>
    /// <remarks>
    /// The <see cref="EntityDatabase"/> forward the message (or command) as en event to all clients subscribed to the message. <br/>
    /// If sending the message to the <see cref="EntityDatabase"/> is successful <see cref="SyncFunction.Success"/> is true. <br/>
    /// <i>Notes:</i>
    /// <list type="bullet">
    ///   <item> Messages in contrast to commands return no result. </item>
    ///   <item> The result of a command is available via <see cref="CommandTask{TResult}.Result"/> </item>
    ///   <item> The response of messages and commands provide no information that they are received as events by subscribed clients. </item>
    /// </list>
    /// </remarks>
    public class MessageTask : SyncTask
    {
        /// <summary>
        /// Restrict the clients receiving the message as an event in case they setup a subscription with <see cref="FlioxClient.SubscribeMessage"/>.
        /// </summary>
        /// <remarks>
        /// A default <see cref="EventTargets"/> instance is not restricted to specific target users, clients or groups. <br/>
        /// So a message is forwarded by the Hub as an event to all clients subscribed to the message. <br/>
        /// </remarks>
        public              EventTargets    EventTargets { get; set; }
        internal            EventTargets    GetOrCreateTargets() => EventTargets ?? (EventTargets = new EventTargets());
        
        internal  readonly  string          name;
        protected readonly  JsonValue       param;
        
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        internal  override  TaskState       State       => state;
        
        public    override  string          Details     => $"MessageTask (name: {name})";
        internal  override  TaskType        TaskType    => TaskType.message;

        
        internal MessageTask(string name, in JsonValue param) {
            this.name       = name;
            this.param      = param;
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            var targets = EventTargets;
            return new SendMessage {
                name        = name,
                param       = param,
                syncTask    = this,
                users       = targets?.users,
                clients     = targets?.clients,
                groups      = targets?.groups
            };
        }
    }
    
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

        public   override   string          Details     => $"CommandTask (name: {name})";

        /// <summary>Return the result of a command used as a command as JSON.
        /// JSON is "null" if the command doesnt return a result.
        /// For type safe access of the result use <see cref="ReadResult{T}"/></summary>
        public              JsonValue       RawResult  => IsOk("CommandTask.RawResult", out Exception e) ? result : throw e;
        
        internal CommandTask(string name, in JsonValue param, Pool pool)
            : base (name, param)
        {
            this.pool = pool;
        }

        /// <summary>
        /// Return a type safe result of a command.
        /// The result is null if the command doesnt return a result.
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
        /// The result is null if the command doesnt return a result.
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
                syncTask    = this,
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
        
        internal CommandTask(string name, in JsonValue param, Pool pool)
            : base (name, param, pool) { }
    }
}

