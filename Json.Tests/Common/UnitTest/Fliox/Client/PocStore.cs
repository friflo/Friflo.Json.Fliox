// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    /// <summary>
    /// The <see cref="PocStore"/> offer two functionalities: <br/>
    /// 1. Defines a database <b>schema</b> by declaring its containers, commands and messages<br/>
    /// 2. Is a database <b>client</b> providing type-safe access to its containers, commands and messages <br/>
    /// </summary>
    /// <remarks>
    /// Its containers are fields or properties of type <see cref="EntitySet{TKey,T}"/>. <br/>
    /// Its commands are methods returning a <see cref="CommandTask{TResult}"/>.<br/>
    /// Its messages are methods returning a <see cref="MessageTask"/>.
    /// <see cref="PocStore"/> instances can be used on server and client side.
    /// </remarks>
    [OpenAPI(version : "1.0.0",     termsOfService: "https://github.com/friflo/Friflo.Json.Fliox",
        contactName : "Ullrich Praetz", contactUrl      : "https://github.com/friflo/Friflo.Json.Fliox/issues",
        licenseName : "AGPL-3.0",       licenseUrl      : "https://spdx.org/licenses/AGPL-3.0-only.html")]
    [OpenAPIServer(description : "test localhost",  url : "http://localhost:8010/fliox/rest/main_db")]
    [OpenAPIServer(description : "test 127.0.0.1",  url : "http://127.0.0.1:8010/fliox/rest/main_db")]
    public class PocStore : FlioxClient
    {
        // --- containers
        public readonly EntitySet <string, Order>       orders;
        public readonly EntitySet <string, Customer>    customers;
        public readonly EntitySet <string, Article>     articles;
        /// <summary>ensure multiple containers can use same entity Type</summary> 
        public readonly EntitySet <string, Article>     articles2;
        public readonly EntitySet <string, Producer>    producers;
        public readonly EntitySet <string, Employee>    employees;
        public readonly EntitySet <string, TestType>    types;
        public readonly EntitySet <string, NonClsType>  nonClsTypes;
        public readonly EntitySet <string, TestKeyName> keyName;
        
        
        public PocStore(FlioxHub hub, string dbName = null): base (hub, dbName) {
        }
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private         TestCommands    _test;
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public          TestCommands    test => _test ??= new TestCommands(this);
        
        // --- commands
        [DatabaseCommand              ("TestCommand")]
        public CommandTask<bool>        Test (TestCommand param)    => send.Command<TestCommand, bool>(param, "TestCommand");
        /// <summary> Create the given number of sync requests each with an upsert to articles </summary>
        public CommandTask<int>         MultiRequests(int? param)   => send.Command<int>             ();
        public CommandTask<string>      SyncCommand (string param)  => send.Command<string, string>  (param);
        public CommandTask<string>      AsyncCommand (string param) => send.Command<string, string>  (param);
        public CommandTask<string>      Command1 ()                 => send.Command<string>          ();
        public CommandTask<int>         CommandInt (int param)      => send.Command<int>             ();     // test required param (value type)
        public CommandTask<int[]>       CommandIntArray(int[] param)=> send.Command<int[],int[]>     (param);
        public CommandTask<Article[]>   CommandClassArray(Article[] param)=> send.Command<Article[],Article[]>(param);
        
        // --- messages
        public MessageTask              Message1    (string param)  => send.Message  (param);
        public MessageTask              AsyncMessage(string param)  => send.Message  (param);
        public MessageTask              StartTime   (DateTime param)=> send.Message  (param);
        public MessageTask              StopTime    (DateTime param)=> send.Message  (param);
    }
    
    [MessagePrefix("test.")]
    public class TestCommands : HubMessages
    {
        public TestCommands(FlioxClient client) : base(client) { }
        
        // --- commands
        public CommandTask<string>  Command2 ()                 => send.Command<string>          ();
        public CommandTask<string>  CommandHello (string param) => send.Command<string, string>  (param);
        
        public CommandTask<int>     CommandExecutionError ()    => send.Command<int>             ();     // test returning an error
        public CommandTask<int>     CommandExecutionException() => send.Command<int>             (); // test throwing exception

        
        // --- messages
        public MessageTask          Message2 (string param)     => send.Message                  (param);
        public MessageTask          AsyncMessage3 (string param)=> send.Message                  (param);
    }
}
