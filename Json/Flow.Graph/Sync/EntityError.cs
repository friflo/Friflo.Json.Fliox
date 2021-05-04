// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public class EntityError
    {
        [Fri.Property]  public      EntityErrorType type;
        [Fri.Property]  public      string          error;
        [Fri.Ignore]    public      string          id;
        [Fri.Ignore]    public      string          container;
    
        public string GetMessage() {
            switch (type) {
                case EntityErrorType.ParseError: return $"Failed parsing entity: {container} '{id}', {error}";
                default:
                    return $"EntityError {type} - {container} '{id}', {error}";
            }
        }

        public override     string          ToString() => GetMessage();

        public EntityError() { } // required for TypeMapper

        public EntityError(EntityErrorType type, string container, string  id, string error) {
            this.type       = type;
            this.container  = container;
            this.id         = id;
            this.error      = error;
        }
    }

    public class EntityException : Exception
    {
        public EntityException(EntityError error) : base(error.GetMessage()) { }
    }

    public enum EntityErrorType
    {
        ParseError,
        ReadError,
        WriteError
    }
}
