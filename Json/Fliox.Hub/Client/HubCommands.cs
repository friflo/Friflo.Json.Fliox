// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client
{
    // currently concept validation only
    public class HubCommands
    {
        private readonly    FlioxClient client;
        private readonly    string      domain;
        
        protected HubCommands (FlioxClient client, string domain) {
            this.domain = domain + ".";
            this.client = client;
        }
        
        protected CommandTask<TResult> SendCommand<TParam, TResult>(string name, TParam param) {
            name = domain + name; // todo avoid concatenation?
            return client.SendCommand<TParam, TResult>(name, param);
        }
    }
    
    public class StdCommands : HubCommands
    {
        protected internal StdCommands(FlioxClient client, string domain) : base(client, domain) { }
        
        // Declared only to generate command in Schema 
        internal CommandTask<JsonValue>     DbEcho(JsonValue _) => throw new InvalidOperationException("unexpected call of DbEcho command");

        // --- Db*
        public CommandTask<TParam>          DbEcho<TParam> (TParam param) =>
            SendCommand<TParam,TParam>  (StdCommand.DbEcho, param);
        public CommandTask<DbContainers>    DbContainers()  =>  SendCommand<JsonValue, DbContainers>   (StdCommand.DbContainers,new JsonValue());
        public CommandTask<DbCommands>      DbCommands()    =>  SendCommand<JsonValue, DbCommands>     (StdCommand.DbCommands,  new JsonValue());
        public CommandTask<DbSchema>        DbSchema()      =>  SendCommand<JsonValue, DbSchema>       (StdCommand.DbSchema,    new JsonValue());
        public CommandTask<DbStats>         DbStats()       =>  SendCommand<JsonValue, DbStats>        (StdCommand.DbStats,     new JsonValue());
        
        // --- Hub*
        public CommandTask<HostDetails>     HostDetails()    =>  SendCommand<JsonValue, HostDetails>   (StdCommand.HostDetails,  new JsonValue());
        public CommandTask<HostCluster>     HostCluster()    =>  SendCommand<JsonValue, HostCluster>   (StdCommand.HostCluster,  new JsonValue());
    }
}