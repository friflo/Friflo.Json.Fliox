// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    internal sealed class SyncTransaction
    {
        internal readonly   int         taskIndex;
        
        internal SyncTransaction(int taskIndex) {
            this.taskIndex     = taskIndex;
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