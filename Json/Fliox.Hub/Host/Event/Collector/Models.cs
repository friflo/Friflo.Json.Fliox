// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Host.Event.Collector
{
    public sealed class WriteTaskModel
    {
        public          JsonValue       task;
        public          JsonKey         cont;
        public          List<JsonValue> set;

        public override string          ToString() => $"'{cont}' {task} count: {set.Count}";

        internal void Set(in JsonValue taskType, in JsonKey container, List<JsonValue> entities) {
            task    = taskType;
            cont    = container;
            set     = entities;
        }
    }
    
    public sealed class DeleteTaskModel
    {
        public          JsonValue       task;
        public          JsonKey         cont;
        public          List<JsonKey>   ids;
        
        public override string          ToString() => $"'{cont}' {task} count: {ids.Count}";
        
        internal void Set(in JsonValue taskType, in JsonKey container, List<JsonKey> keys) {
            task    = taskType;
            cont    = container;
            ids     = keys;
        }
    }
}