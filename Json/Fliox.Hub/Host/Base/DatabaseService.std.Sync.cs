// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.SQL;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public partial class DatabaseService
    {
        private static Result<TransactionResult> TransactionBegin (Param<Empty> param, MessageContext context) {
            var taskIndex   = context.task.intern.index;
            var result      = context.syncContext.Transaction(TransCommand.Begin, taskIndex);
            if (result.error == null) {
                return SyncTransaction.CreateResult(TransCommand.Rollback);
            }
            return Result.Error(result.error);
        }
        
        private static Result<TransactionResult> TransactionCommit (Param<Empty> param, MessageContext context) {
            var taskIndex   = context.task.intern.index;
            var result      = context.syncContext.Transaction(TransCommand.Commit, taskIndex);
            if (result.error == null) {
                return SyncTransaction.CreateResult(result.state);
            }
            return Result.Error(result.error);
        }
        
        private static Result<TransactionResult> TransactionRollback (Param<Empty> param, MessageContext context) {
            var taskIndex   = context.task.intern.index;
            var result      = context.syncContext.Transaction(TransCommand.Rollback, taskIndex);
            if (result.error == null) {
                return SyncTransaction.CreateResult(TransCommand.Rollback);
            }
            return Result.Error(result.error);
        }
    }
}