// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using static System.Diagnostics.DebuggerBrowsableState;

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

        public EntityValue(in JsonKey key, JsonValue json) {
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
}