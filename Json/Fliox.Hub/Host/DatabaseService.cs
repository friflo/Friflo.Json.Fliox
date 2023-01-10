// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Internal;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper.Map;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable UseDeconstruction
namespace Friflo.Json.Fliox.Hub.Host
{
    public  delegate  void    HostMessageHandler<TParam>              (Param<TParam> param, MessageContext context);
    public  delegate  Task    HostMessageHandlerAsync<TParam>         (Param<TParam> param, MessageContext context);
    public  delegate  TResult HostCommandHandler<TParam, out TResult> (Param<TParam> param, MessageContext context);
    

    /// <summary>
    /// A <see cref="DatabaseService"/> is attached to every <see cref="EntityDatabase"/> to handle all
    /// <see cref="SyncRequest.tasks"/> of a <see cref="SyncRequest"/>.
    /// </summary>
    /// <remarks>
    /// Each task is either a database operation, a command or a message.
    /// <list type="bullet">
    ///   <item>
    ///     <b>Database operations</b> are a build-in functionality of every <see cref="EntityDatabase"/>.
    ///     These operations are:
    ///     <see cref="CreateEntities"/>, <see cref="UpsertEntities"/>, <see cref="DeleteEntities"/>,
    ///     <see cref="MergeEntities"/>, <see cref="ReadEntities"/> or <see cref="QueryEntities"/>.
    ///   </item>
    ///   <item>
    ///     An application can add <b>command</b> handlers to perform custom operations.
    ///     Each command is a tuple of its name and param. See <see cref="SendCommand"/>.
    ///     Its command handler must return a result. See <see cref="SendCommandResult"/>. 
    ///   </item>
    ///   <item>
    ///     Similar to commands an application can add <b>message</b> handlers to process events or notifications.
    ///     Each message is a tuple of its name and param. See <see cref="SendMessage"/>.
    ///     In contrast to commands message handlers return void (nothing).
    ///   </item>
    /// </list>
    /// </remarks>
    public partial class DatabaseService
    {
        [DebuggerBrowsable(Never)]
        private readonly    Dictionary<string, MessageDelegate>     handlers = new Dictionary<string, MessageDelegate>();
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             IReadOnlyCollection<MessageDelegate>    Handlers => handlers.Values;
        
        internal readonly   ConcurrentQueue<RequestJob>             requestQueue;
        public              bool                                    QueueRequestExecution   => requestQueue != null;
        private  const      bool                                    RunOnCallingThread      =  true;
        
        /// <summary>
        /// If <paramref name="queueRequests"/> is true <see cref="SyncRequest"/> are queued for execution otherwise
        /// they are executed as they arrive.
        /// </summary>
        /// <remarks>
        /// To execute queued requests (<paramref name="queueRequests"/> is true) <see cref="ExecuteQueuedRequests"/>
        /// need to be called regularly.<br/>
        /// This enables requests / task execution on the calling thread. <br/>
        /// This mode guarantee sequential execution of messages, commands and container operations like
        /// read, query, create, upsert, merge and delete.<br/>
        /// So using lock's or other thread synchronisation mechanisms are not necessary.
        /// </remarks> 
        public DatabaseService (bool queueRequests = false) {
            AddStdCommandHandlers();
            requestQueue = queueRequests ? new ConcurrentQueue<RequestJob>() : null;
        }
        
        protected internal virtual void PreExecuteTasks (SyncContext syncContext)  { }
        protected internal virtual void PostExecuteTasks(SyncContext syncContext)  { }
        
        protected internal virtual void CustomizeUpsert (UpsertEntities task, SyncContext syncContext) { }
        protected internal virtual void CustomizeMerge  (MergeEntities  task, SyncContext syncContext) { }
        
        public virtual async Task<SyncTaskResult> ExecuteTaskAsync (SyncRequestTask task, EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (!AuthorizeTask(task, syncContext, out var error))
                return error;
            return await task.ExecuteAsync(database, response, syncContext).ConfigureAwait(false);
        }
        
        public virtual            SyncTaskResult  ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (!AuthorizeTask(task, syncContext, out var error))
                return error;
            return task.Execute(database, response, syncContext);
        }
        
        protected static bool AuthorizeTask(SyncRequestTask task, SyncContext syncContext, out SyncTaskResult error) {
            var taskAuthorizer = syncContext.authState.taskAuthorizer;
            if (taskAuthorizer.AuthorizeTask(task, syncContext)) {
                error = null;
                return true;
            }
            var sb = new StringBuilder(); // todo StringBuilder could be pooled
            sb.Append("not authorized");
            var authError = syncContext.authState.error; 
            if (authError != null) {
                sb.Append(". ");
                sb.Append(authError);
            }
            var anonymous = syncContext.hub.Authenticator.anonymousUser;
            var user = syncContext.User;
            if (user != anonymous) {
                sb.Append(". user: ");
                sb.Append(user.userId);
            }
            var message = sb.ToString();
            error = SyncRequestTask.PermissionDenied(message);
            return false;
        }
        
        
        // ------------------- API to add instance / static and synchronous / async methods -------------------
        /// <summary>
        /// Add a synchronous message handler method with a method signature like:
        /// <code>
        /// void TestMessage(Param&lt;string&gt; param, MessageContext context) { ... }
        /// </code>
        /// message handler methods can be static or instance methods.
        /// </summary>
        protected void AddMessageHandler<TParam> (string name, HostMessageHandler<TParam> handler) {
            var message = new MessageDelegate<TParam>(name, handler);
            handlers.Add(name, message);
        }
        
        /// <summary>
        /// Add an asynchronous message handler method with a method signature like:
        /// <code>
        /// Task TestMessage(Param&lt;TestCommand&gt; param, MessageContext context) { ... }
        /// </code>
        /// message handler methods can be static or instance methods.
        /// </summary>
        protected void AddMessageHandlerAsync<TParam> (string name, HostMessageHandlerAsync<TParam> handler) {
            var message = new MessageDelegateAsync<TParam>(name, handler);
            handlers.Add(name, message);
        }
        
        /// <summary>
        /// Add a synchronous command handler method with a method signature like:
        /// <code>
        /// bool TestCommand(Param&lt;TestCommand&gt; param, MessageContext context) { ... }
        /// </code>
        /// command handler methods can be static or instance methods.
        /// </summary>
        protected void AddCommandHandler<TParam, TResult> (string name, HostCommandHandler<TParam, TResult> handler) {
            var command = new CommandDelegate<TParam, TResult>(name, handler);
            handlers.Add(name, command);
        }

        /// <summary>
        /// Add an asynchronous command handler method with a method signature like:
        /// <code>
        /// Task&lt;bool&gt; TestCommand(Param&lt;TestCommand&gt; param, MessageContext context) { ... }
        /// </code>
        /// command handler methods can be static or instance methods.
        /// </summary>
        protected void AddCommandHandlerAsync<TParam, TResult> (string name, HostCommandHandler<TParam, Task<TResult>> handler) {
            var command = new CommandDelegateAsync<TParam, TResult>(name, handler);
            handlers.Add(name, command);
        }
       
        /// <summary>
        /// Add all methods of the given class <paramref name="instance"/> with the parameters <br/>
        /// (<see cref="Param{TParam}"/> param, <see cref="MessageContext"/> context) as a message/command handler. <br/>
        /// A command handler has return type - a message handler returns void. <br/>
        /// Command handler example:
        /// <code>
        /// bool TestCommand(Param&lt;TestCommand&gt; param, MessageContext context) { ... }
        /// </code>
        /// Message handler methods can be: <br/>
        /// - static or instance methods <br/>
        /// - synchronous or asynchronous - using <see cref="Task{TResult}"/> as return type.
        /// </summary>
        /// <param name="instance">the instance of class containing message handler methods.
        ///     Commonly the instance of a <see cref="DatabaseService"/></param>
        /// <param name="messagePrefix">the prefix of a message/command - e.g. "test."; null or "" to add messages without prefix</param>
        protected void AddMessageHandlers<TClass>(TClass instance, string messagePrefix) where TClass : class
        {
            var type                = typeof(TClass);
            var handlerInfos        = DatabaseServiceUtils.GetHandlers(type);
            if (handlerInfos == null)
                return;

            foreach (var handler in handlerInfos) {
                MessageDelegate messageDelegate;
                if (handler.resultType == typeof(void)) {
                    messageDelegate = CreateMessageCallback(instance, handler, messagePrefix);
                } else {
                    messageDelegate = CreateCommandCallback(instance, handler, messagePrefix);
                }
                handlers.Add(messageDelegate.name, messageDelegate);
            }
        }
        
        private static string GetHandlerName(HandlerInfo handler, string messagePrefix) {
            if (string.IsNullOrEmpty(messagePrefix))
                return handler.name;
            return $"{messagePrefix}{handler.name}";
        }
        
        private static MessageDelegate  CreateMessageCallback<TClass>(
            TClass      handlerClass,
            HandlerInfo handler,
            string      messagePrefix) where TClass : class
        {
            var genericArgs         = new Type[1];
            var constructorParams   = new object[2];
            // if (handler.name == "DbContainers") { int i = 1; }
            genericArgs[0]          = handler.valueType;
            var genericTypeArgs     = typeof(HostMessageHandler<>).MakeGenericType(genericArgs);
            var firstArgument       = handler.method.IsStatic ? null : handlerClass;
            var handlerDelegate     = Delegate.CreateDelegate(genericTypeArgs, firstArgument, handler.method);

            constructorParams[0]    = GetHandlerName(handler, messagePrefix);
            constructorParams[1]    = handlerDelegate;
            object instance = TypeMapperUtils.CreateGenericInstance(typeof(MessageDelegate<>),      genericArgs, constructorParams);   
            return (MessageDelegate)instance;
        }
        
        private static MessageDelegate  CreateCommandCallback<TClass>(
            TClass      handlerClass,
            HandlerInfo handler,
            string      messagePrefix) where TClass : class
        {
            var genericArgs         = new Type[2];
            var constructorParams   = new object[2];
            // if (handler.name == "DbContainers") { int i = 1; }
            genericArgs[0]          = handler.valueType;
            genericArgs[1]          = handler.resultType;
            var genericTypeArgs     = typeof(HostCommandHandler<,>).MakeGenericType(genericArgs);
            var firstArgument       = handler.method.IsStatic ? null : handlerClass;
            var handlerDelegate     = Delegate.CreateDelegate(genericTypeArgs, firstArgument, handler.method);

            constructorParams[0]    = GetHandlerName(handler, messagePrefix);
            constructorParams[1]    = handlerDelegate;
            object instance;
            // is return type of command handler of type: Task<TResult> ?  (==  is async command handler)
            if (handler.resultTaskType != null) {
                genericArgs[1] = handler.resultTaskType;
                instance = TypeMapperUtils.CreateGenericInstance(typeof(CommandDelegateAsync<,>), genericArgs, constructorParams);
            } else {
                instance = TypeMapperUtils.CreateGenericInstance(typeof(CommandDelegate<,>),      genericArgs, constructorParams);    
            }
            return (MessageDelegate)instance;
        }
        
        // --- internal API ---
        internal bool TryGetMessage(string name, out MessageDelegate message) {
            return handlers.TryGetValue(name, out message);
        }
        
        internal string[] GetMessages() {
            var count   = CountMessageTypes(MsgType.Message);
            var result  = new string[count];
            int         n = 0;
            foreach (var pair in handlers) {
                if (pair.Value.MsgType == MsgType.Message)
                    result[n++] = pair.Key;
            }
            return result;
        }
        
        internal string[] GetCommands() {
            var count   = CountMessageTypes(MsgType.Command);
            var result  = new string[count];
            int n       = 0;
            // add std. commands on the bottom
            AddCommands(result, ref n, false, handlers);
            AddCommands(result, ref n, true,  handlers);
            return result;
        }
        
        private int CountMessageTypes (MsgType msgType) {
            int count = 0;
            foreach (var pair in handlers) {
                if (pair.Value.MsgType == msgType) count++;
            }
            return count;
        }
        
        private static void AddCommands (string[] commands, ref int n, bool standard, Dictionary<string, MessageDelegate> commandMap) {
            foreach (var pair in commandMap) {
                if (pair.Value.MsgType != MsgType.Command)
                    continue;
                var name = pair.Key;
                if (name.StartsWith("std.") == standard)
                    commands[n++] = name;
            }
        }
        
        /// <summary>
        /// Execute queued tasks in case request queueing is enabled in the <see cref="DatabaseService"/> constructor
        /// </summary>
        // ReSharper disable MethodHasAsyncOverload
        public async Task ExecuteQueuedRequests() {
            while(true) {
                if (!requestQueue.TryDequeue(out var job))
                    return;
                var syncRequest = job.syncRequest;
                ExecuteSyncResult response;
                if (syncRequest.intern.executionType == ExecutionType.Sync) {
                    response =       job.hub.ExecuteRequest     (syncRequest, job.syncContext);
                } else {
                    response = await job.hub.ExecuteRequestAsync(syncRequest, job.syncContext).ConfigureAwait(RunOnCallingThread);
                }
                job.taskCompletionSource.SetResult(response);
            }
        }
    }
    
    internal readonly struct RequestJob
    {
        internal readonly FlioxHub                                  hub;
        internal readonly SyncRequest                               syncRequest;
        internal readonly SyncContext                               syncContext;
        internal readonly TaskCompletionSource<ExecuteSyncResult>   taskCompletionSource;
            
        internal RequestJob(
            FlioxHub    hub,
            SyncRequest syncRequest,
            SyncContext syncContext)
        {
            this.hub                = hub;
            this.syncRequest        = syncRequest;
            this.syncContext        = syncContext;
            taskCompletionSource    = new TaskCompletionSource<ExecuteSyncResult>();
        }
    }
}