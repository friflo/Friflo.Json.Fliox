// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using static NUnit.Framework.Assert;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class PocService : DatabaseService {
        private   readonly  TestHandlerScan        test    = new TestHandlerScan();
        internal  readonly  TestHandlerManual      manual  = new TestHandlerManual();
        private   readonly  EmptyHandler           empty   = new EmptyHandler();
        
        public PocService() {
            // add all command handlers of the passed handler classes
            AddMessageHandlers(this,    "");
            AddMessageHandlers(test,    "test.");
            AddMessageHandlers(empty,   "empty.");
            
            // add command handlers individually
            AddCommandHandler       <string,string>         ("SyncCommand",         TestHandlerManual.SyncCommand);
            AddCommandHandlerAsync  <string,string>         ("AsyncCommand",        TestHandlerManual.AsyncCommand);
            AddCommandHandler       <string,string>         ("Command1",            manual.Command1);
            AddCommandHandler       <int,   int>            ("CommandInt",          manual.CommandInt);
            AddMessageHandler       <string>                ("Message1",            manual.Message1);
            AddMessageHandlerAsync  <string>                ("AsyncMessage",        manual.AsyncMessage);
            AddCommandHandler       <int[], int[]>          ("CommandIntArray",     manual.CommandIntArray);
            AddCommandHandler       <Article[], Article[]>  ("CommandClassArray",   manual.CommandClassArray);
        }
        
        private static bool TestCommand(Param<TestCommand> param, MessageContext command) {
            AreEqual("TestCommand", command.Name);
            AreEqual("TestCommand", command.ToString());
            command.WriteNull = true; // ensure API available
            return true;
        }
        
        private async Task<int> MultiRequests(Param<int?> param, MessageContext command) {
            param.Get(out int? count, out var _);
            if (count == null) count = 100;
            var store = new PocStore(command.Hub) { UserInfo = command.UserInfo };
            store.StartTime(DateTime.Now);
            var article = new Article { id = "1111", name = "MultiRequests"};
            for (int i = 0; i < count; i++) {
                store.articles.Upsert(article);
                await store.SyncTasks();
            }
            store.StopTime(DateTime.Now);
            await store.SyncTasks();
            return count.Value;
        }
    }
    
    /// <summary>
    /// Uses to show adding all its command handlers by <see cref="DatabaseService.AddMessageHandlers{TClass}"/>
    /// </summary>
    public class TestHandlerScan {
        private string message2 = "nothing received";
            
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
        
        private static int CommandExecutionError(Param<string> param, MessageContext command) {
            command.Error("test command execution error");
            return -1;
        }
        
        private static int CommandExecutionException(Param<string> param, MessageContext command) {
            throw new InvalidOperationException("test command throw exception");
        }
    }
    
    /// <summary>
    /// Uses to show adding its command handlers manual by <see cref="DatabaseService.AddCommandHandler{TParam,TResult}"/>
    /// or <see cref="DatabaseService.AddCommandHandlerAsync{TParam,TResult}"/>
    /// </summary>
    public class TestHandlerManual {
        private     string  message1Param     = "nothing received";
        private     string  asyncMessageParam = "nothing received";
        
        public      string  AsyncMessageParam => asyncMessageParam;
        
        public static string SyncCommand(Param<string> param, MessageContext command) {
            return "hello SyncCommand";
        }
        
        public static Task<string> AsyncCommand(Param<string> param, MessageContext command) {
            return Task.FromResult("hello AsyncCommand");
        }
        
        public string Command1(Param<string> param, MessageContext command) {
            return message1Param;
        }
        
        public int CommandInt(Param<int> param, MessageContext command) {
            if (!param.Get(out int value, out var error))
                command.Error(error);
            return value;
        }
        
        public void Message1(Param<string> param, MessageContext command) {
            param.Get(out message1Param, out _);
        }
        
        public Task AsyncMessage(Param<string> param, MessageContext command) {
            param.Get(out asyncMessageParam, out _);
            return Task.CompletedTask;
        }
        
        public int[] CommandIntArray(Param<int[]> param, MessageContext command) {
            if (!param.Get(out int[] value, out var error)) {
                command.Error(error);
            }
            if (value == null)
                return new int[] { 1, 2, 3 };
            return value;
        }
        
        public Article[] CommandClassArray(Param<Article[]> param, MessageContext command) {
            if (!param.Get(out Article[] value, out var error)) {
                command.Error(error);
            }
            if (value == null)
                return new Article[] { new Article { id = "foo", name = "bar" } };
            return value;
        }
    }
    
    public class EmptyHandler { }
}