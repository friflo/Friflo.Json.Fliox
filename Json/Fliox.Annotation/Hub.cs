// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Friflo.Json.Fliox
{
    // --- database Attributes
    // used by Friflo.Json.Fliox.Hub
    
    // -------------------------------- class attributes ------------------------------
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MessagePrefixAttribute : Attribute {
        public MessagePrefixAttribute (string prefix) { }
    }
    
    // -------------------------------- field & property attributes ------------------------------
    /* /// <summary> Declare the attributed member is the primary key of an entity in its container </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class PrimaryKeyAttribute : Attribute {  } */

    /// <summary> Declare the attributed member as an auto increment field / property </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AutoIncrementAttribute : Attribute {
    }
    
    /// <summary> Specify that the attributed member is a reference (secondary key) to an entity in the given container </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RelationAttribute : Attribute {
        public RelationAttribute (string container) {}
    }
    
    // -------------------------------- method attributes ------------------------------
    /// <summary> Set a custom command name for the attributed method </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DatabaseCommandAttribute : Attribute {
        public DatabaseCommandAttribute (string name) { Name = name; }
        public string       Name        { get; }
    }
    
    /// <summary>
    /// Declare the attributed method as a command handler.<br/>
    /// Signature of command handler methods<br/>
    /// <list type="bullet">
    ///   <item><i>synchronous</i>
    ///     <code>
    ///     [CommandHandler]
    ///     Result&lt;TResult&gt; MyCommand(Param&lt;TParam&gt; param, MessageContext context) { }
    ///     </code>
    ///   </item>
    ///   <item><i>asynchronous</i>
    ///     <code>
    ///     [CommandHandler]
    ///     async Task&lt;Result&lt;TResult&gt;&gt; MyCommand(Param&lt;TParam&gt; param, MessageContext context) { }
    ///     </code>
    ///   </item>
    /// </list>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CommandHandlerAttribute : Attribute {
        public CommandHandlerAttribute (string name = null) { Name = name; }
        public string       Name        { get; }
    }
    
    /// <summary>
    /// Declare the attributed method as a message handler.<br/>
    /// Signature of message handler methods<br/>
    /// <list type="bullet">
    ///   <item><i>synchronous</i>
    ///     <code>
    ///     [MessageHandler]
    ///     void MyMessage(Param&lt;TParam&gt; param, MessageContext context) { }
    ///     </code>
    ///   </item>
    ///   <item><i>asynchronous</i>
    ///     <code>
    ///     [MessageHandler]
    ///     async Task MyMessage(Param&lt;TParam&gt; param, MessageContext context) { }
    ///     </code>
    ///   </item>
    /// </list>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MessageHandlerAttribute : Attribute {
        public MessageHandlerAttribute (string name = null) { Name = name; }
        public string       Name        { get; }
    }
}