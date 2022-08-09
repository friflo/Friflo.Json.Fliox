// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
    [OpenAPI(Version = "1.0.0",     TermsOfService  = "https://github.com/friflo/Friflo.Json.Fliox",
        ContactName = "Ullrich Praetz", ContactUrl      = "https://github.com/friflo/Friflo.Json.Fliox/issues",
        LicenseName = "AGPL-3.0",       LicenseUrl      = "https://spdx.org/licenses/AGPL-3.0-only.html")]
    [OpenAPIServer(Description = "test localhost",  Url = "http://localhost:8010/fliox/rest/main_db")]
    [OpenAPIServer(Description = "test 127.0.0.1",  Url = "http://127.0.0.1:8010/fliox/rest/main_db")]
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
        
        
        public PocStore(FlioxHub hub, string dbName = null): base (hub, dbName) {
            test = new TestCommands(this);
        }
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly TestCommands    test;
        
        // --- commands
        [DatabaseCommand(Name =        "TestCommand")]
        public CommandTask<bool>        Test (TestCommand param)    => SendCommand<TestCommand, bool>("TestCommand",param);
        public CommandTask<string>      SyncCommand (string param)  => SendCommand<string, string>  ("SyncCommand", param);
        public CommandTask<string>      AsyncCommand (string param) => SendCommand<string, string>  ("AsyncCommand",param);
        public CommandTask<string>      Command1 ()                 => SendCommand<string>          ("Command1");
        public CommandTask<int>         CommandInt (int param)      => SendCommand<int>             ("CommandInt");     // test required param (value type)
        
        // --- messages
        public MessageTask              Message1    (string param)  => SendMessage  ("Message1",        param);
        public MessageTask              AsyncMessage(string param)  => SendMessage  ("AsyncMessage",    param);
    }
    
    
    public class TestCommands : HubMessages
    {
        public TestCommands(FlioxClient client) : base(client) { }
        
        // --- commands
        public CommandTask<string>  Command2 ()                 => SendCommand<string>          ("test.Command2");
        public CommandTask<string>  CommandHello (string param) => SendCommand<string, string>  ("test.CommandHello", param);
        
        public CommandTask<int>     CommandExecutionError ()    => SendCommand<int>             ("test.CommandExecutionError");     // test returning an error
        public CommandTask<int>     CommandExecutionException() => SendCommand<int>             ("test.CommandExecutionException"); // test throwing exception

        
        // --- messages
        public MessageTask          Message2 (string param)     => SendMessage                  ("test.Message2",  param);
    }
}
