// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Mapper.Map.Utils;

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
        /// <summary> Used to send typed messages / commands by classes extending <see cref="HubMessages"/></summary>
        protected readonly FlioxClient.SendTask send;
        
        protected HubMessages (FlioxClient client) {
            var prefix  = GetMessagePrefix();
            send        = new FlioxClient.SendTask(client, prefix);
        }
        
        private string GetMessagePrefix() {
            var     type = GetType();
            string  prefix;
            lock (PrefixCache) {
                if (PrefixCache.TryGetValue(type, out prefix)) {
                    return prefix;
                }
            }
            var attributes  = type.CustomAttributes;
            prefix          = MessageUtils.GetMessagePrefix(attributes);
            lock (PrefixCache) {
                PrefixCache.TryAdd(type, prefix);    
            }
            return prefix;
        }
        
        private static readonly Dictionary<Type, string> PrefixCache = new Dictionary<Type, string>();
    }
    
    // ---------------------------------- standard commands ----------------------------------
    /// <summary>
    /// Contains standard database commands. Its commands are prefixed with <b>std.*</b>
    /// </summary>
    [MessagePrefix("std.")]
    public sealed class StdCommands : HubMessages
    {
        internal StdCommands(FlioxClient client) : base(client) { }
        
        // Declared only to generate command in Schema
        /// <summary>Echos the given parameter to assure the database is working appropriately. </summary>
        internal CommandTask<JsonValue> Echo(JsonValue _) => throw new InvalidOperationException("unexpected call of DbEcho command");

        // --- commands: database
        /// <summary>Echos the given parameter to assure the database is working appropriately. </summary>
        public CommandTask<TParam>      Echo<TParam> (TParam param) => send.Command<TParam,TParam>   (param);
        /// <summary>A command that completes after a specified number of milliseconds. </summary>
        public CommandTask<int>         Delay(int delay)        => send.Command<int,int>                 (delay);
        /// <summary>List all database containers</summary>
        public CommandTask<DbContainers>Containers()            => send.Command<DbContainers>            ();
        /// <summary>List all database commands and messages</summary>
        public CommandTask<DbMessages>  Messages()              => send.Command<DbMessages>              ();
        /// <summary>Return the Schema assigned to the database</summary>
        public CommandTask<DbSchema>    Schema()                => send.Command<DbSchema>                ();
        /// <summary>Return the number of entities of all containers (or the given container) of the database</summary>
        public CommandTask<DbStats>     Stats(string param)     => send.Command<DbStats>                 ();
        
        // --- commands: host
        /// <summary>Returns general information about the Hub like version, host, project and environment name</summary>
        public CommandTask<HostInfo>    Host(HostParam param)   => send.Command<HostParam, HostInfo>    (param);
        /// <summary>List all databases and their containers hosted by the Hub</summary>
        public CommandTask<HostCluster> Cluster()               => send.Command<HostCluster>            ();
        
        // --- commands: user
        /// <summary>Return the groups of the current user. Optionally change the groups of the current user</summary>
        public CommandTask<UserResult>  User(UserParam param)   => send.Command<UserParam,UserResult>  (param);
        
        // --- commands: client
        /// <summary>Return client specific infos and adjust general client behavior like <see cref="ClientParam.queueEvents"/></summary>
        public CommandTask<ClientResult> Client(ClientParam param)=> send.Command<ClientParam, ClientResult>(param);

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