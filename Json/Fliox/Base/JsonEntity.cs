// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    public readonly struct JsonEntity
    {
        public  readonly    JsonKey     key;
        public  readonly    JsonValue   value;

        public  override    string      ToString() => GetString();

        public JsonEntity (in JsonValue value) {
            key         = default;
            this.value  = value;
        }
        
        public JsonEntity (in JsonKey key, in JsonValue value) {
            this.key    = key;
            this.value  = value;
        }
        
        private string GetString() {
            if (key.IsNull())
                return $"value: {value}";
            return $"key: {key}";
        }
    }
}