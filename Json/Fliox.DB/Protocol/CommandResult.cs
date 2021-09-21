// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol
{
    public interface ICommandResult
    {
        /// In case a command fails its <see cref="CommandError.message"/> is assigned to <see cref="TaskErrorResult.message"/>
        [Fri.Ignore]
        CommandError                Error { get; set;  }
    }
    
    public class CommandError // : SyncTaskResult
    {
        public              string      message;

        public   override   string      ToString() => message;
        
        public CommandError() {}
        public CommandError(string message) {
            this.message = message;
        }
    }
}