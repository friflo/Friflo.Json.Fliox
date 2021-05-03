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
        [Fri.Ignore]    public      string      Json => error == null ? value.json : throw error;
        [Fri.Ignore]    private     EntityError error;

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
    }
    
    public class EntityError : Exception
    {
        public readonly     string      container;
        public readonly     string      id;
        public readonly     string      message;

        public override     string      Message => $"Failed parsing entity: {container} '{id}', {message}";
        public override     string      ToString() => Message;

        public EntityError(string container, string  id, string message) {
            this.container  = container;
            this.id         = id;
            this.message    = message;
        }
    }
}