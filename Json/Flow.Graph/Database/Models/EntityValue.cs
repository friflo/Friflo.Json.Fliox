// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Database.Models
{
    public class EntityValue
    {
        [Fri.Property]  private     JsonValue   value;
        [Fri.Ignore]    private     EntityError error;
        
        [Fri.Ignore]    public      string      Json    => error == null ? value.json : throw error;
        [Fri.Ignore]    public      EntityError Error => error;

        public override             string      ToString() => Json;

        public void SetJson(string json) {
            value.json = json;
        }
        
        public void SetError(EntityError error) {
            this.error = error;
        }

        public EntityValue() { } // required for TypeMapper

        public EntityValue(string json) {
            value.json = json;
        }
        
        public EntityValue(EntityError error) {
            this.error = error;
        }
    }
    
    public class EntityError : Exception
    {
        [Fri.Property]  public      EntityErrorType type;
        [Fri.Property]  public      string          message;
        [Fri.Ignore]    public      string          id;
        [Fri.Ignore]    public      string          container;
    
        public override     string          Message {
            get {
                switch (type) {
                    case EntityErrorType.ParseError: return $"Failed parsing entity: {container} '{id}', {message}";
                    default:
                        return $"EntityError {type} - {container} '{id}', {message}";
                }
            }
        }

        public override     string          ToString() => Message;

        public EntityError() { } // required for TypeMapper

        public EntityError(EntityErrorType type, string container, string  id, string message) {
            this.type       = type;
            this.container  = container;
            this.id         = id;
            this.message    = message;
        }
    }

    public enum EntityErrorType
    {
        ParseError,
        ReadError,
        WriteError
    }
}