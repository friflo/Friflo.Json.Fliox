// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// <see cref="MessageContext"/> expose all data relevant for command execution as properties or methods.
    /// </summary>
    /// <remarks>
    /// In general it provides:
    /// - the command <see cref="Name"/> == method name <br/>
    /// - the <see cref="Database"/> instance <br/>
    /// - the <see cref="Hub"/> exposing general Hub information <br/>
    /// - a <see cref="Pool"/> mainly providing common utilities to transform JSON <br/>
    /// For consistency the API to access the command param is same a <see cref="IMessage"/>
    /// </remarks>
    public sealed class MessageContext { // : IMessage { // uncomment to check API consistency
        public              string          Name            => nameShort.AsString();
        public              SyncRequestTask Task            { get; }
        public              FlioxHub        Hub             => syncContext.hub;
        public              IHubLogger      Logger          => syncContext.hub.Logger;
        public              EntityDatabase  Database        => syncContext.database;            // not null
        public              User            User            => syncContext.User;
        public              ShortString     ClientId        => syncContext.clientId;
        public              UserInfo        UserInfo        => GetUserInfo();
        
        public              bool            WriteNull       { get; set; }
        public              bool            WritePretty     { get; set; }

        // --- internal / private properties
        internal            Pool            Pool            => syncContext.pool;
        [DebuggerBrowsable(Never)]
        internal            SyncContext     SyncContext     => syncContext;
        
        // --- internal / private fields
        [DebuggerBrowsable(Never)]
        private  readonly   ShortString     nameShort;
        [DebuggerBrowsable(Never)]
        private  readonly   SyncContext     syncContext;
        internal            string          error;
        internal            TaskErrorType   errorType;
        
        public   override   string          ToString()      => nameShort.AsString();


        internal MessageContext(SyncRequestTask task, in ShortString name, SyncContext syncContext) {
            Task                = task;
            nameShort           = name;
            this.syncContext    = syncContext;
            WritePretty         = true;
        }
        
        /// <summary>Set result of <see cref="MessageContext"/> execution to an error</summary>
        public void Error(string message) {
            error       = message ?? throw new ArgumentNullException(nameof(message));
            errorType   = TaskErrorType.CommandError;
        }
        
        public void ValidationError(string message) {
            error       = message ?? throw new ArgumentNullException(nameof(message));
            errorType   = TaskErrorType.ValidationError;
        }

        private UserInfo GetUserInfo() { 
            var user = syncContext.User;
            return new UserInfo (user.userId, user.token, syncContext.clientId);
        }
    }
}