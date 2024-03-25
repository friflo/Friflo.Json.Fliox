// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    /// <summary>
    /// Contains either a <see cref="SyncTaskResult"/> in case the query / mutation method is valid
    /// or an <see cref="error"/> in case of an invalid query / mutation.
    /// </summary>
    internal readonly struct QueryRequest {
        internal  readonly  SyncRequestTask task;
        internal  readonly  bool            selectAll;
        internal  readonly  QueryError?     error;

        public override string ToString() {
            if (error != null)
                return error.Value.message;
            return task.ToString();
        }

        internal QueryRequest (SyncRequestTask task, bool? selectAll = false) {
            this.task       = task;
            this.selectAll  = selectAll.HasValue && selectAll.Value;
            this.error      = null;
        }
        
        private QueryRequest (in QueryError? error) {
            this.task       = null;
            this.selectAll  = false;
            this.error      = error;
        }
        
        public static implicit operator QueryRequest(QueryError? error) {
            return new QueryRequest(error);
        }
    }
    
    internal readonly struct QueryError {
        internal  readonly  string      argName;
        internal  readonly  string      message;

        public    override  string      ToString() => message;
        
        internal QueryError (string argName, string message) {
            this.argName    = argName;
            this.message    = message;
        }
    }
}