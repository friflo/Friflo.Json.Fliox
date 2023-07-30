// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Host.Utils;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable MergeConditionalExpression
namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    public readonly struct EntityValue
    {
        [Ignore]    public      readonly    JsonKey     key;
        [DebuggerBrowsable(Never)]     
        [Serialize] private     readonly    JsonValue   value;
        [DebuggerBrowsable(Never)]
        [Serialize] private     readonly    EntityError error;
        
        [Ignore]                public      JsonValue   Json        => error == null ? value : throw new EntityException(error);
        [Ignore]                public      EntityError Error       => error;

        public override         string      ToString()  => GetString();

        public EntityValue(in JsonKey key) {
            this.key    = key;
            value       = default;
            error       = null;
        }

        public EntityValue(in JsonKey key, in JsonValue json) {
            this.key    = key;
            value       = json;
            error       = null;
        }
        
        public EntityValue(in JsonKey key, EntityError error) {
            this.key    = key;
            this.error  = error;
            value       = default;
        }
        
        private string GetString() {
            if (error == null) {
                return $"{key}  {value}";
            }
            return $"{key}  {error.type}: {error.message}";
        }
    }
    
    public readonly struct EntityObject
    {
        [Ignore]    public  readonly    JsonKey     key;
        [Ignore]    public  readonly    object      obj;

                    public  override    string      ToString() => key.AsString();

        public EntityObject(JsonKey key, object obj) {
            this.key    = key;
            this.obj    = obj;
        }
    }
    
    /// <summary>
    /// Contains an array of tuples: (<see cref="JsonKey"/>, <see cref="JsonValue"/>)<br/>
    /// </summary>
    /// <remarks>
    /// Two aspects of this class<br/>
    /// - avoid passing array parameters: <c>EntityValue[]</c><br/>
    /// - be prepared to support array of tuples: (<see cref="JsonKey"/>, <see cref="object"/>)
    /// </remarks>
    public readonly struct Entities
    {
        public  readonly    EntityValue[]   values;
        public  readonly    EntityObject[]  objects;
        public              int             Length  => values != null ? values.Length : objects.Length;
        public  readonly    ContainerType   containerType;

        public override     string          ToString() => $"Length: {Length}";

        public  Entities (EntityValue[] values) {
            this.values     = values;
            this.objects    = null;
            containerType   = ContainerType.Values;
        }
        
        public  Entities (EntityObject[] objects) {
            this.values     = null;
            this.objects    = objects;
            containerType   = ContainerType.Objects;
        }
        
        public void EntitiesToJson(
            out ListOne<JsonValue>  set,
            out List<JsonKey>       notFound,
            out List<EntityError>   errors)
        {
            set         = new ListOne<JsonValue>(values.Length);
            errors      = null;
            notFound    = null;
            foreach (var value in values) {
                if (value.Json.IsNull()) {
                    notFound ??= new List<JsonKey>();
                    notFound.Add(value.key);
                    continue;
                } 
                var error = value.Error;
                if (error == null) {
                    set.Add(value.Json);
                    continue;
                }
                errors ??= new List<EntityError>();
                errors.Add(error);
            }
        }
        
        public static EntityValue[] JsonToEntities(
            ListOne<JsonValue>  set,
            List<JsonKey>       notFound,
            List<EntityError>   errors,
            EntityProcessor     processor,
            string              keyName)    
        {
            var values = new EntityValue[set.Count + (notFound?.Count ?? 0) + (errors?.Count ?? 0)];
            var index = 0;
            foreach (var value in set.GetReadOnlySpan()) {
                if (processor.GetEntityKey(value, keyName, out var key, out var error)) {
                    values[index++] = new EntityValue(key, value);
                } else {
                    throw new InvalidOperationException($"missing key int result: {error}");
                }
            }
            if (notFound != null) {
                foreach (var key in notFound) {
                    values[index++] = new EntityValue(key);
                }
            }
            if (errors != null) {
                foreach (var error in errors) {
                    values[index++] = new EntityValue(error.id, error);
                }
            }
            return values;
        } 
    }
}