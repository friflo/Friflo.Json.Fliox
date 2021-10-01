// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Text;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol
{
    /// An <see cref="EntityError"/> needs to be set only, if the access to <see cref="EntityValue"/>'s
    /// returned by a previous call to <see cref="EntityContainer.ReadEntities"/> or
    /// <see cref="EntityContainer.QueryEntities"/> fails.
    /// This implies that the previous read or query call was successful. 
    public sealed class EntityError
    {
        [Fri.Required]      public  EntityErrorType     type;
        [Fri.Property]      public  string              message;
            
        [Fri.Ignore]        public  JsonKey             id;
        [Fri.Ignore]        public  string              container;
        /// <summary>Is != <see cref="TaskErrorResultType.None"/> if the error is caused indirectly by a <see cref="SyncRequestTask"/> error.</summary>
        [Fri.Ignore]        public  TaskErrorResultType taskErrorType;
        /// <summary>Show the stacktrace if <see cref="taskErrorType"/> == <see cref="TaskErrorResultType.UnhandledException"/>
        /// and the accessed <see cref="EntityDatabase"/> expose this data.</summary>
        [Fri.Ignore]        public  string              stacktrace;

        public override     string              ToString() => AsText(true);

        public EntityError() { } // required for TypeMapper

        public EntityError(EntityErrorType type, string container, in JsonKey id, string message) {
            this.type       = type;
            this.container  = container;
            this.id         = id;
            this.message    = message;
        }
        
        public string AsText(bool showStack) {
            var sb = new StringBuilder();
            AppendAsText("", sb, showStack);
            return sb.ToString();
        }

        public void AppendAsText(string prefix, StringBuilder sb, bool showStack) {
            sb.Append(prefix);
            sb.Append(type);
            sb.Append(": ");
            sb.Append(container);
            sb.Append(" [");
            id.AppendTo(sb);
            sb.Append("], ");
            if (taskErrorType != TaskErrorResultType.None) {
                sb.Append(taskErrorType);
                sb.Append(" - ");
            }
            sb.Append(message);
            if (showStack && stacktrace != null) {
                sb.Append('\n');
                sb.Append(stacktrace);
            }
        }
    }

    public class EntityException : Exception
    {
        public EntityException(EntityError error) : base(error.AsText(false)) { }
    }

    public enum EntityErrorType
    {
        Undefined,   // Prevent implicit initialization of underlying value 0 to a valid value (ParseError) 
        ParseError,
        ReadError,
        WriteError,
        DeleteError,
        PatchError
    }
}
