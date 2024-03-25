// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

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
    public readonly struct MessageContext { // : IMessage { // uncomment to check API consistency
        public          string          Name        => task.name.AsString();
        public          SyncMessageTask Task        => task;
        public          FlioxHub        Hub         => syncContext.hub;
        public          IHubLogger      Logger      => syncContext.hub.Logger;
        public          EntityDatabase  Database    => syncContext.database;            // not null
        public          User            User        => syncContext.User;
        public          ShortString     ClientId    => syncContext.clientId;
        public          UserInfo        UserInfo    => GetUserInfo();

        public override string          ToString()  => task.name.AsString();

        // --- internal / private fields
        [Browse(Never)] internal readonly   SyncContext     syncContext;
        [Browse(Never)] internal readonly   SyncMessageTask task;

        internal MessageContext(SyncMessageTask task, SyncContext syncContext) {
            this.task           = task;
            this.syncContext    = syncContext;
        }

        private UserInfo GetUserInfo() { 
            var user = syncContext.User;
            return new UserInfo (user.userId, user.token, syncContext.clientId);
        }
    }
}