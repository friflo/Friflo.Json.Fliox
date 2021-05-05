// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public class EntityError
    {
        [Fri.Property]  public  EntityErrorType type;
        [Fri.Property]  public  string          message;
        [Fri.Ignore]    public  string          id;
        [Fri.Ignore]    public  string          container;
    
        public                  string          AsText() => $"{type} - {container} '{id}', {message}";
        public override         string          ToString() => AsText();

        public EntityError() { } // required for TypeMapper

        public EntityError(EntityErrorType type, string container, string  id, string message) {
            this.type       = type;
            this.container  = container;
            this.id         = id;
            this.message    = message;
        }
    }

    public class EntityException : Exception
    {
        public EntityException(EntityError error) : base(error.AsText()) { }
    }

    public enum EntityErrorType
    {
        ParseError,
        ReadError,
        WriteError
    }
}
