// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    /// <summary>
    /// Used by <see cref="SyncResponse"/> to return errors when mutating an entity by: create, upsert, patch and delete
    /// </summary>
    /// <remarks> 
    /// An <see cref="EntityError"/> needs to be set only, if the access to <see cref="EntityValue"/>'s
    /// returned by a previous call to <see cref="EntityContainer.ReadEntitiesAsync"/> or
    /// <see cref="EntityContainer.QueryEntitiesAsync"/> fails.
    /// This implies that the previous read or query call was successful.
    /// </remarks> 
    public sealed class EntityError
    {
        /// <summary>entity id</summary>
        [Required]  public  JsonKey             id;
        /// <summary>error type when accessing an entity in a database</summary>
        [Required]  public  EntityErrorType     type;
        /// <summary>error details when accessing an entity</summary>
        [Serialize] public  string              message;
            
        [Ignore]    public  ShortString         container;
        /// <summary>Is != <see cref="TaskErrorType.None"/> if the error is caused indirectly by a <see cref="SyncRequestTask"/> error.</summary>
        [Ignore]    public  TaskErrorType       taskErrorType;
        /// <summary>Show the stacktrace if <see cref="taskErrorType"/> == <see cref="TaskErrorType.UnhandledException"/>
        /// and the accessed <see cref="EntityContainer"/> implementation expose this data.</summary>
        [Ignore]    public  string              stacktrace;

        public override     string              ToString() => AsText(true);

        public EntityError() { } // required for TypeMapper

        public EntityError(EntityErrorType type, in ShortString container, in JsonKey id, string message) {
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
            container.AppendTo(sb);
            sb.Append(" [");
            id.AppendTo(sb);
            sb.Append("], ");
            if (taskErrorType != TaskErrorType.None) {
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

    public sealed class EntityException : Exception
    {
        public EntityException(EntityError error) : base(error.AsText(false)) { }
    }

    /// <summary>
    /// Error type when accessing an entity from a database container  
    /// </summary>
    public enum EntityErrorType
    {
        Undefined   = 0,   // Prevent implicit initialization of underlying value 0 to a valid value (ParseError)
        /// <summary>Invalid JSON when reading an entity from database<br/>
        /// can happen with key-value databases - e.g. file-system - as their values are not restricted to JSON</summary>
        ParseError  = 1,
        /// <summary>Reading an entity from database failed<br/>
        /// e.g. a corrupt file when using the file-system as database</summary>
        ReadError   = 2,
        /// <summary>Writing an entity to database failed<br/>
        /// e.g. the file is already in use by another process when using the file-system as database</summary>
        WriteError  = 3,
        /// <summary>Deleting an entity in database failed<br/>
        /// e.g. the file is already in use by another process when using the file-system as database</summary>
        DeleteError = 4,
        /// <summary>Patching an entity failed</summary>
        PatchError  = 5
    }
}
