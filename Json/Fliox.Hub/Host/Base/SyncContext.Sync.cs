// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host.SQL;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public sealed partial class SyncContext
    {
        
        internal TransResult Transaction(TransCommand command, int taskIndex) {
            var db      = Database;
            var trans   = transaction;
            switch (command)
            {
                case TransCommand.Begin: {
                    if (trans != null) {
                        return new TransResult("Transaction already started");
                    }
                    transaction = new SyncTransaction(taskIndex);
                    return db.Transaction(this, TransCommand.Begin);
                }
                case TransCommand.Commit: {
                    if (trans == null) {
                        return new TransResult("Missing begin transaction");
                    }
                    transaction = null;
                    bool success = !SyncTransaction.HasTaskErrors(response.tasks, trans.beginTask + 1, taskIndex);
                    TransResult result;
                    if (success) {
                        result = db.Transaction(this, TransCommand.Commit);
                        if (result.error != null) {
                            result = db.Transaction(this, TransCommand.Rollback);
                        }
                    } else {
                        result = db.Transaction(this, TransCommand.Rollback);
                    }
                    UpdateTransactionBeginResult(trans.beginTask, result);
                    return result;
                }
                case TransCommand.Rollback: {
                    if (trans == null) {
                        return new TransResult("Missing begin transaction");
                    }
                    transaction = null;
                    var result = db.Transaction(this, TransCommand.Rollback);
                    UpdateTransactionBeginResult(trans.beginTask, result);
                    return result;
                }
            }
            return new TransResult($"invalid transaction command: {command}");
        }
    }
}
