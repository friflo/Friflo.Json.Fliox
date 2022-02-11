// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class PocHandler : TaskHandler {
        private readonly TestCommandsHandler    test    = new TestCommandsHandler();
        private readonly TestCommandsHandler2   test2   = new TestCommandsHandler2();
        private readonly EmptyCommandsHandler   empty   = new EmptyCommandsHandler();
        
        public PocHandler() {
            AddCommandHandlers(this, "");
            AddCommandHandlers(test, "test.");
            AddCommandHandlers(test, "empty.");
            
            AddCommand      <string,string>("SyncCommand",  TestCommandsHandler2.SyncCommand);
            AddCommandAsync <string,string>("AsyncCommand", TestCommandsHandler2.AsyncCommand);
        }
        
        private static bool TestCommand(Command<TestCommand> command) {
            return true;
        }
    }
    
    /// <summary>
    /// Uses to show adding all its command handlers by <see cref="TaskHandler.AddCommandHandlers{TClass}"/>
    /// </summary>
    public class TestCommandsHandler {
        private static string Command1(Command<string> command) {
            return "hello Command1";
        }
        
        private static string Command2(Command<string> command) {
            return "hello Command2";
        }
    }
    
    /// <summary>
    /// Uses to show adding its command handlers individually by <see cref="TaskHandler.AddCommand{TParam,TResult}"/>
    /// or <see cref="TaskHandler.AddCommandAsync{TParam,TResult}"/>
    /// </summary>
    public class TestCommandsHandler2 {
        public static string SyncCommand(Command<string> command) {
            return "hello SyncCommand";
        }
        
        public static Task<string> AsyncCommand(Command<string> command) {
            return Task.FromResult("hello AsyncCommand");
        }
    }
    
    public class EmptyCommandsHandler { }
}