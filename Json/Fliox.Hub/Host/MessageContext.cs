// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Mapper;

using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// <see cref="CommandContext"/> expose all data relevant for command execution as properties or methods. <br/>
    /// - the command <see cref="Name"/> == method name <br/>
    /// - the <see cref="DatabaseName"/> <br/>
    /// - the <see cref="Database"/> instance <br/>
    /// - the <see cref="Hub"/> exposing general Hub information <br/>
    /// - a <see cref="Pool"/> mainly providing common utilities to transform JSON <br/>
    /// </summary>
    public class MessageContext {
        public              string          Name            { get; }
        public              IPool           Pool            => executeContext.pool;
        public              FlioxHub        Hub             => executeContext.hub;
        public              string          DatabaseName    => executeContext.DatabaseName;
        public              EntityDatabase  Database        => executeContext.Database;
        public              User            User            => executeContext.User;
        public              JsonKey         ClientId        => executeContext.clientId;
        
        [DebuggerBrowsable(Never)]  internal            ExecuteContext  ExecuteContext  => executeContext;
        [DebuggerBrowsable(Never)]  protected readonly  ExecuteContext  executeContext;

        public   override   string          ToString()      => Name;
        
        internal MessageContext(string name, ExecuteContext executeContext) {
            Name                = name;
            this.executeContext = executeContext;
        }
    }
    
    /// <summary>
    /// <see cref="CommandContext"/> expose all data relevant for command execution as properties or methods. <br/>
    /// - the command <see cref="MessageContext.Name"/> == method name <br/>
    /// - the <see cref="MessageContext.DatabaseName"/> <br/>
    /// - the <see cref="MessageContext.Database"/> instance <br/>
    /// - the <see cref="MessageContext.Hub"/> exposing general Hub information <br/>
    /// - a <see cref="MessageContext.Pool"/> mainly providing common utilities to transform JSON <br/>
    /// </summary>
    /// <remarks>For consistency the API to access the command param is same a <see cref="IMessage"/></remarks>
    public class CommandContext : MessageContext { // : IMessage { // uncomment to check API consistency

        public              bool            WriteNull       { get; set; }
        public              bool            WritePretty     { get; set; }
        
        internal            string          error;

        public              UserInfo        UserInfo { get {
            var user = executeContext.User;
            return new UserInfo (user.userId, user.token, executeContext.clientId);
        } }

        internal CommandContext(string name, ExecuteContext executeContext) : base (name, executeContext) {
            WritePretty         = true;
        }
        
        /// <summary>Set result of <see cref="CommandContext"/> execution to an error</summary>
        public void Error(string message) {
            error = message;
        }

        /// <summary>Set result of <see cref="CommandContext"/> execution to an error. <br/>
        /// It returns the default value of the given <typeparamref name="TResult"/> to simplify
        /// returning from a command handler with a single statement like:
        /// <code>
        /// if (!command.ValidateParam(out var param, out var error))
        ///     return command.Error &lt;int&gt;(error);
        /// </code>  
        /// </summary>
        public TResult Error<TResult>(string message) {
            error = message;
            return default;
        }
    }
}