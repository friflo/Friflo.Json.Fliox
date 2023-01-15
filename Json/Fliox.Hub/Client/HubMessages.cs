// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.DB.Cluster;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Used to group message/command methods by a single class.
    /// </summary>
    /// <remarks>
    /// Message/command methods can be added directly to a <see cref="FlioxClient"/> sub class.
    /// When adding many methods it can cause confusion between <see cref="FlioxClient"/> own methods and the message/command methods.
    /// The intention is to use a sub class of <see cref="HubMessages"/> as a field in a class extending <see cref="FlioxClient"/>.
    /// This establish differentiation between <see cref="FlioxClient"/> own methods and message/command methods added
    /// to a <see cref="FlioxClient"/> sub class.
    /// <code >
    /// public class TestStore : FlioxClient
    /// {
    ///     // --- commands
    ///     public MyCommands test;
    ///     
    ///     public TestStore(FlioxHub hub) : base(hub) {
    ///         test = new MyCommands(this);
    ///     }
    /// }
    /// 
    /// public class MyCommands : HubMessages
    /// {
    ///     public MyCommands(FlioxClient client) : base(client) { }
    ///     
    ///     public CommandTask &lt;string&gt; Cmd (string param) => SendCommand &lt;string, string&gt;("test.Cmd", param);
    /// }
    /// </code>
    /// </remarks>
    public class HubMessages
    {
        private readonly    FlioxClient client;
        
        protected HubMessages (FlioxClient client) {
            this.client = client;
        }
        
        protected MessageTask SendMessage<TParam>(string name, TParam param) {
            return client.SendMessage(name, param);
        }
        
        protected CommandTask<TResult> SendCommand<TResult>(string name) {
            return client.SendCommand<TResult>(name);
        }
        
        protected CommandTask<TResult> SendCommand<TParam, TResult>(string name, TParam param) {
            return client.SendCommand<TParam, TResult>(name, param);
        }
    }
    
    // ---------------------------------- standard commands ----------------------------------
    /// <summary>
    /// Contains standard database commands. Its commands are prefixed with <b>std.*</b>
    /// </summary>
    public sealed class StdCommands : HubMessages
    {
        internal StdCommands(FlioxClient client) : base(client) { }
        
        // Declared only to generate command in Schema
        /// <summary>echos the given parameter to assure the database is working appropriately. </summary>
        internal CommandTask<JsonValue> Echo(JsonValue _) => throw new InvalidOperationException("unexpected call of DbEcho command");

        // --- commands: database
        /// <summary>echos the given parameter to assure the database is working appropriately. </summary>
        public CommandTask<TParam>      Echo<TParam> (TParam param) => SendCommand<TParam,TParam>   (Std.Echo, param);
        /// <summary>A a command that completes after a specified number of milliseconds. </summary>
        public CommandTask<int>         Delay(int delay)    => SendCommand<int,int>                 (Std.Delay, delay);
        /// <summary>list all database containers</summary>
        public CommandTask<DbContainers>Containers()        => SendCommand<DbContainers>            (Std.Containers);
        /// <summary>list all database commands and messages</summary>
        public CommandTask<DbMessages>  Messages()          => SendCommand<DbMessages>              (Std.Messages);
        /// <summary>return the Schema assigned to the database</summary>
        public CommandTask<DbSchema>    Schema()            => SendCommand<DbSchema>                (Std.Schema);
        /// <summary>return the number of entities of all containers (or the given container) of the database</summary>
        public CommandTask<DbStats>     Stats(string param) => SendCommand<DbStats>                 (Std.Stats);
        
        // --- commands: host
        /// <summary>returns general information about the Hub like version, host, project and environment name</summary>
        public CommandTask<HostInfo>    Host(HostParam param)=> SendCommand<HostParam, HostInfo>    (Std.HostInfo, param);
        /// <summary>list all databases and their containers hosted by the Hub</summary>
        public CommandTask<HostCluster> Cluster()           => SendCommand<HostCluster>             (Std.HostCluster);
        
        // --- commands: user
        /// <summary>return the groups of the current user. Optionally change the groups of the current user</summary>
        public CommandTask<UserResult>  User(UserParam param) => SendCommand<UserParam,UserResult>  (Std.User, param);
        
        // --- commands: client
        /// <summary>return client specific infos and adjust general client behavior like <see cref="ClientParam.queueEvents"/></summary>
        public CommandTask<ClientResult> Client(ClientParam param)=> SendCommand<ClientParam, ClientResult>(Std.Client, param);

    }
    
    
    /// Should not be public. commands are prefix with
    /// <b>std.*</b>
    internal static class Std  {
        // --- database
        public const string Echo        = "std.Echo";
        public const string Delay       = "std.Delay";
        public const string Containers  = "std.Containers";
        public const string Messages    = "std.Messages";
        public const string Schema      = "std.Schema";
        public const string Stats       = "std.Stats";
        public const string Client      = "std.Client";

        // --- host
        public const string HostInfo    = "std.Host";
        public const string HostCluster = "std.Cluster";
        
        // --- user
        public const string User        = "std.User";
    }
}