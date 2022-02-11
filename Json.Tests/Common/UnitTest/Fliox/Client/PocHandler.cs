// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class PocHandler : TaskHandler {
        private readonly TestCommandsHandler    test    = new TestCommandsHandler();
        private readonly TestCommandsHandler2   test2    = new TestCommandsHandler2();
        private readonly EmptyCommandsHandler   empty   = new EmptyCommandsHandler();
        
        public PocHandler() {
            AddCommandHandlers(this, "");
            AddCommandHandlers(test, "test.");
            AddCommandHandlers(test, "empty.");
            
            AddCommand      <string,string>("SyncCommand",  null, TestCommandsHandler2.SyncCommand);
            AddCommandAsync <string,string>("AsyncCommand", null, TestCommandsHandler2.AsyncCommand);
        }
        
        private static bool TestCommand(Command<TestCommand> command) {
            return true;
        }
    }
    
    public class TestCommandsHandler {
        private static string Command1(Command<string> command) {
            return "hello Command1";
        }
        
        private static string Command2(Command<string> command) {
            return "hello Command2";
        }
    }
    
    public class EmptyCommandsHandler {
    }
    
    public class TestCommandsHandler2 {
        public static string SyncCommand(Command<string> command) {
            return "hello SyncCommand";
        }
        
        public static Task<string> AsyncCommand(Command<string> command) {
            return Task.FromResult("hello AsyncCommand");
        }
    }
    
    
}