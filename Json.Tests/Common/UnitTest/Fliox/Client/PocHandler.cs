// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using static NUnit.Framework.Assert;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class PocHandler : TaskHandler {
        private readonly TestHandlerScan        test    = new TestHandlerScan();
        private readonly TestHandlerManual      manual  = new TestHandlerManual();
        private readonly EmptyHandler           empty   = new EmptyHandler();
        
        public PocHandler() {
            // add all command handlers of the passed handler classes
            AddMessageHandlers(this,    "");
            AddMessageHandlers(test,    "test.");
            AddMessageHandlers(empty,   "empty.");
            
            // add command handlers individually
            AddCommandHandler       <string,string> ("SyncCommand",     TestHandlerManual.SyncCommand);
            AddCommandHandlerAsync  <string,string> ("AsyncCommand",    TestHandlerManual.AsyncCommand);
            AddCommandHandler       <string,string> ("Command1",        manual.Command1);
            AddMessageHandler       <string>        ("Message1",        manual.Message1);
        }
        
        private static bool TestCommand(Param<TestCommand> param, MessageContext command) {
            AreEqual("TestCommand", command.Name);
            AreEqual("TestCommand", command.ToString());
            command.WriteNull = true; // ensure API available
            return true;
        }
    }
    
    /// <summary>
    /// Uses to show adding all its command handlers by <see cref="TaskHandler.AddMessageHandlers{TClass}"/>
    /// </summary>
    public class TestHandlerScan {
        private string message2;
            
        private void Message2(Param<string> param, MessageContext command) {
            param.Get(out message2, out _);
        }
        
        private string Command2(Param<string> param, MessageContext command) {
            return message2;
        }
        
        private static string CommandHello(Param<string> param, MessageContext command) {
            param.Get(out var result, out _);
            return result;
        }
    }
    
    /// <summary>
    /// Uses to show adding its command handlers manual by <see cref="TaskHandler.AddCommandHandler{TParam,TResult}"/>
    /// or <see cref="TaskHandler.AddCommandHandlerAsync{TParam,TResult}"/>
    /// </summary>
    public class TestHandlerManual {
        private string message1;
        
        public static string SyncCommand(Param<string> param, MessageContext command) {
            return "hello SyncCommand";
        }
        
        public static Task<string> AsyncCommand(Param<string> param, MessageContext command) {
            return Task.FromResult("hello AsyncCommand");
        }
        
        public string Command1(Param<string> param, MessageContext command) {
            return message1;
        }
        
        public void Message1(Param<string> param, MessageContext command) {
            param.Get(out message1, out _);
        }
    }
    
    public class EmptyHandler { }
}