// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    public struct JsonEntity
    {
        public  JsonKey     key;
        public  JsonValue   value;
        
        public JsonEntity (JsonValue value) {
            key         = default;
            this.value  = value;
        }
    }
}