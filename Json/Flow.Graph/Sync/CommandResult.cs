// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public interface ICommandResult
    {
        [Fri.Property(Name = "error")]
        CommandError                Error { get; set;  }
    }
    
    public class CommandError
    {
        public          string      message;

        public override string      ToString() => message;
    }
}