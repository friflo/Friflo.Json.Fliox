// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.DB.Cluster;

namespace Friflo.Json.Fliox.Hub.Client
{
    // ---------------------------------- standard commands ----------------------------------
    /// <summary>
    /// <see cref="StdCommands"/> contains all standard database commands. Its commands are prefixed with <b>std.*</b><br/>
    /// Each method creates a <see cref="CommandTask"/> and they are executed as a request
    /// when calling <see cref="FlioxClient.SyncTasks"/>.
    /// </summary>
    [MessagePrefix("std.")]
    public sealed class StdCommands : HubMessages
    {
        internal StdCommands(FlioxClient client) : base(client) { }
        
        // Declared only to generate command in Schema
        /// <summary>Echos the given parameter to assure the database is working appropriately. </summary>
        internal CommandTask<JsonValue>         Echo(JsonValue _) => throw new InvalidOperationException("unexpected call of DbEcho command");

        // --- commands: database
        /// <summary>Echos the given parameter to assure the database is working appropriately. </summary>
        public CommandTask<TParam>              Echo<TParam> (TParam param) => send.Command<TParam,TParam>   (param);

        /// <summary>A command that completes after a specified number of milliseconds. </summary>
        public CommandTask<int>                 Delay(int delay)            => send.Command<int,int>(delay);

        /// <summary>List all database containers</summary>
        public CommandTask<DbContainers>        Containers()                => send.Command<DbContainers>();

        /// <summary>List all database commands and messages</summary>
        public CommandTask<DbMessages>          Messages()                  => send.Command<DbMessages>();

        /// <summary>Return the Schema assigned to the database</summary>
        public CommandTask<DbSchema>            Schema()                    => send.Command<DbSchema>();

        /// <summary>Return the number of entities of all containers (or the given container) of the database</summary>
        public CommandTask<DbStats>             Stats(string param)         => send.Command<DbStats>();

        /// <summary>Begin a transaction containing all subsequent <see cref="SyncTask"/>'s.<br/>
        /// The transaction ends by either calling <see cref="FlioxClient.SyncTasks"/> or explicit by
        /// <see cref="TransactionCommit"/> / <see cref="TransactionRollback"/></summary>
        public CommandTask<TransactionResult>   TransactionBegin()          => send.Command<TransactionResult>();

        /// <summary>Commit a transaction started previously with <see cref="TransactionBegin"/></summary>
        public CommandTask<TransactionResult>   TransactionCommit()         => send.Command<TransactionResult>();

        /// <summary>Rollback a transaction started previously with <see cref="TransactionBegin"/></summary>
        public CommandTask<TransactionResult>   TransactionRollback()       => send.Command<TransactionResult>();
        
        /// <summary>Execute a raw SQL query / statement</summary>
        public CommandTask<RawSqlResult>        ExecuteRawSQL(RawSql sql)   => send.Command<RawSql, RawSqlResult>(sql);

        // --- commands: host
        /// <summary>Returns general information about the Hub like version, host, project and environment name</summary>
        public CommandTask<HostInfo>            Host(HostParam param)       => send.Command<HostParam, HostInfo>(param);
        
        /// <summary>List all databases and their containers hosted by the Hub</summary>
        public CommandTask<HostCluster>         Cluster()                   => send.Command<HostCluster>();
        
        // --- commands: user
        /// <summary>Return the groups of the current user. Optionally change the groups of the current user</summary>
        public CommandTask<UserResult>          User(UserParam param)       => send.Command<UserParam,UserResult>(param);
        
        // --- commands: client
        /// <summary>Return client specific infos and adjust general client behavior like <see cref="ClientParam.queueEvents"/></summary>
        public CommandTask<ClientResult>        Client(ClientParam param)  => send.Command<ClientParam, ClientResult>(param);
    }
}