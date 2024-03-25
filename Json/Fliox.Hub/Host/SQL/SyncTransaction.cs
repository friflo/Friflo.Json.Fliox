// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    internal sealed class SyncTransaction
    {
        internal readonly   int     beginTask;
        
        internal SyncTransaction(int beginTask) {
            this.beginTask     = beginTask;
        }
        
        internal static bool HasTaskErrors(ListOne<SyncTaskResult> taskResults, int from, int to) {
            for (int index = from; index < to; index++) {
                var taskResult = taskResults[index];
                if (taskResult.Failed) {
                    return true;
                }
            }
            return false;
        }
        
        
        internal static TransactionResult CreateResult(TransCommand command) {
            var executed = command switch {
                TransCommand.Commit     => TransactionCommand.Commit,
                TransCommand.Rollback   => TransactionCommand.Rollback,
                _                       => throw new InvalidOperationException($"unexpected case: {command}")
            };
            return new TransactionResult { executed = executed };
        }
    }
    
    public enum TransCommand {
        Begin       = 0,
        Commit      = 1,
        Rollback    = 2,
    }
    
    public sealed class TransResult {
        public readonly string          error;
        public readonly TransCommand    state;
        
        public TransResult(string error) {
            this.error = error ?? throw new ArgumentNullException(nameof(error));
        }
        
        public TransResult(TransCommand  state) {
            this.state = state;
        }
    }
}