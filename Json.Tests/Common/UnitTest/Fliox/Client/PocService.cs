// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Host;
using static NUnit.Framework.Assert;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class PocService : DatabaseService {
        internal  readonly  TestHandlerManual      manual  = new TestHandlerManual();
        
        public PocService() {
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
        
        [CommandHandler]
        private static Result<bool> TestCommand(Param<TestCommand> param, MessageContext context) {
            AreEqual("TestCommand", context.Name);
            AreEqual("TestCommand", context.ToString());
            return true;
        }
        
        [CommandHandler]
        private async Task<Result<int>> MultiRequests(Param<int?> param, MessageContext context) {
            param.Get(out int? count, out var _);
            if (count == null) count = 100;
            var store = new PocStore(context.Hub) { UserInfo = context.UserInfo };
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
        
        private string message2 = "nothing received";
        
        // --- Note:
        //  following handler methods were declared in a separate class before: class TestHandlerScan.
        //  Removed this feature to simplify implementation and perception.
        //  Now all handlers methods must be declared in a single class. 
        [MessageHandler("test.Message2")]
        private void Message2(Param<string> param, MessageContext context) {
            param.Get(out message2, out _);
        }
        
        [MessageHandler("test.AsyncMessage3")]
        private Task AsyncMessage3(Param<string> param, MessageContext context) {
            return Task.CompletedTask;
        }
        
        [CommandHandler("test.Command2")]
        private Result<string> Command2(Param<string> param, MessageContext context) {
            return message2;
        }
        
        [CommandHandler("test.CommandHello")]
        private static Result<string> CommandHello(Param<string> param, MessageContext context) {
            param.Get(out var result, out _);
            return result;
        }
        
        [CommandHandler("test.CommandExecutionError")]
        private static Result<int> CommandExecutionError(Param<string> param, MessageContext context) {
            return Result.Error("test command execution error");
        }
        
        [CommandHandler("test.CommandExecutionException")]
        private static Result<int> CommandExecutionException(Param<string> param, MessageContext context) {
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
        
        public static Result<string> SyncCommand(Param<string> param, MessageContext context) {
            return "hello SyncCommand";
        }
        
        public static Task<Result<string>> AsyncCommand(Param<string> param, MessageContext context) {
            return Result.TaskError<string>("hello AsyncCommand");
        }
        
        public Result<string> Command1(Param<string> param, MessageContext context) {
            return message1Param;
        }
        
        public Result<int> CommandInt(Param<int> param, MessageContext context) {
            if (!param.Get(out int value, out var error)) {
                return Result.ValidationError(error);
            }
            return value;
        }
        
        public void Message1(Param<string> param, MessageContext context) {
            param.Get(out message1Param, out _);
        }
        
        public Task AsyncMessage(Param<string> param, MessageContext context) {
            param.Get(out asyncMessageParam, out _);
            return Task.CompletedTask;
        }
        
        public Result<int[]> CommandIntArray(Param<int[]> param, MessageContext context) {
            if (!param.Get(out int[] value, out var error)) {
                return Result.ValidationError(error);
            }
            if (value == null)
                return new int[] { 1, 2, 3 };
            return value;
        }
        
        public Result<Article[]> CommandClassArray(Param<Article[]> param, MessageContext context) {
            if (!param.Get(out Article[] value, out var error)) {
                return Result.ValidationError(error);
            }
            if (value == null)
                return new Article[] { new Article { id = "foo", name = "bar" } };
            return value;
        }
    }
}