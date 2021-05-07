// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public interface ICommandResult
    {
        /// In case a command fails its <see cref="CommandError.message"/> is assigned to <see cref="TaskError.message"/>
        [Fri.Ignore]
        CommandError                Error { get; set;  }
    }
    
    public class CommandError
    {
        public          string      message;

        public override string      ToString() => message;
    }
}