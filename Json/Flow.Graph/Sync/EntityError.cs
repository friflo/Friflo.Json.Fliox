// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Text;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    /// An <see cref="EntityError"/> needs to be set only, if the access to <see cref="EntityValue"/>'s
    /// returned by a previous call to <see cref="EntityContainer.ReadEntities"/> or
    /// <see cref="EntityContainer.QueryEntities"/> fails.
    /// This implies that the previous read or query call was successful. 
    public class EntityError
    {
        [Fri.Property]  public  EntityErrorType     type;
        [Fri.Property]  public  string              message;
            
        [Fri.Ignore]    public  string              id;
        [Fri.Ignore]    public  string              container;
        [Fri.Ignore]    public  TaskErrorResultType taskErrorType;

        public override         string              ToString() => AsText();

        public EntityError() { } // required for TypeMapper

        public EntityError(EntityErrorType type, string container, string  id, string message) {
            this.type       = type;
            this.container  = container;
            this.id         = id;
            this.message    = message;
        }
        
        public string AsText() {
            var sb = new StringBuilder();
            AppendAsText(sb);
            return sb.ToString();
        }

        public void AppendAsText(StringBuilder sb) {
            sb.Append(type);
            sb.Append(": ");
            sb.Append(container);
            sb.Append(" '");
            sb.Append(id);
            sb.Append("', ");
            if (taskErrorType != TaskErrorResultType.None) {
                sb.Append(taskErrorType);
                sb.Append(" - ");
            }
            sb.Append(message);
        }
    }

    public class EntityException : Exception
    {
        public EntityException(EntityError error) : base(error.AsText()) { }
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
