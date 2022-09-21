// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    public interface ICommandResult
    {
        /// In case a command fails its <see cref="CommandError.message"/> is assigned to <see cref="TaskErrorResult.message"/>
        [Ignore]    CommandError        Error { get; set;  }
    }
    
    /// <summary>
    /// Note: A <see cref="CommandError"/> is never serialized.
    /// Its fields are assigned to <see cref="TaskErrorResult"/> which instead is used for serialization of errors.
    /// </summary>
    public sealed class CommandError
    {
        [Ignore]    internal    TaskErrorResultType type;
        [Ignore]    internal    string              message;

        public      override    string              ToString() => message;
        
        public CommandError() {}
        public CommandError(string message) {
            this.type       = TaskErrorResultType.DatabaseError;
            this.message    = message;
        }
        public CommandError(TaskErrorResultType type, string message) {
            this.type       = type;
            this.message    = message;
        }
    }
}