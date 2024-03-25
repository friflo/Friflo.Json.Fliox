// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// A class implementing <see cref="IServiceCommands"/> is used to add custom command handler methods
    /// annotated with <b><c>[CommandHandler]</c></b> to an <see cref="EntityDatabase"/>
    /// using <see cref="EntityDatabase.AddCommands"/>. E.g. <br/>
    /// 
    /// <code>
    ///     [CommandHandler]
    ///     async Task&lt;Result&lt;TResult&gt;&gt; MyCommand(Param&lt;TParam&gt; param, MessageContext context)
    /// </code>
    /// <br/>
    /// <see cref="IServiceCommands"/> are added to a database using <see cref="EntityDatabase.AddCommands"/>
    /// </summary>
    /// 
    /// <remarks>
    /// Additional to commands a class implementing <see cref="IServiceCommands"/> can also be used to declare message handler methods. E.g.<br/>
    /// <code>
    ///     [MessageHandler]
    ///     void MyMessage(Param&lt;TParam&gt; param, MessageContext context) { }
    /// </code>
    /// <br/>
    /// <i>Note</i>: Message handler methods - in contrast to command handlers - doesn't return a result.<br/>
    /// </remarks>
    public interface IServiceCommands { }
}