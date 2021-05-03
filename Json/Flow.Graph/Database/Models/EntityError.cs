// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Database.Models
{
    public class EntityError : Exception
    {
        [Fri.Property]  public      EntityErrorType type;
        [Fri.Property]  public      string          error;
        [Fri.Ignore]    public      string          id;
        [Fri.Ignore]    public      string          container;
    
        public override     string          Message {
            get {
                switch (type) {
                    case EntityErrorType.ParseError: return $"Failed parsing entity: {container} '{id}', {error}";
                    default:
                        return $"EntityError {type} - {container} '{id}', {error}";
                }
            }
        }

        public override     string          ToString() => Message;

        public EntityError() { } // required for TypeMapper

        public EntityError(EntityErrorType type, string container, string  id, string error) {
            this.type       = type;
            this.container  = container;
            this.id         = id;
            this.error      = error;
        }
    }

    public enum EntityErrorType
    {
        ParseError,
        ReadError,
        WriteError
    }
}
