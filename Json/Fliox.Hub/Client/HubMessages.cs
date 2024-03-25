// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

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
    ///     public CommandTask &lt;string&gt; Cmd (string param) => send.Command &lt;string, string&gt;(param);
    /// }
    /// </code>
    /// </remarks>
    public class HubMessages
    {
        /// <summary> Used to send typed messages / commands by classes extending <see cref="HubMessages"/></summary>
        protected readonly FlioxClient.SendTask send;
        
        protected HubMessages (FlioxClient client) {
            var type    = GetType();
            var prefix  = HubMessagesUtils.GetMessagePrefix(type);
            send        = new FlioxClient.SendTask(client, prefix);
        }
    }
}