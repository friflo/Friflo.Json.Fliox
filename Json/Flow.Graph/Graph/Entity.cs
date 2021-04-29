// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Flow.Graph
{
    public class Entity
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _id;
        
        public string id {
            get => _id;
            set {
                if (_id == value)
                    return;
                if (_id == null) {
                    _id = value;
                    return;
                }
                throw new InvalidOperationException($"Entity id must not be changed. Type: {GetType()}, id: {_id}, used: {value}");
            }
        }

        public override     string  ToString() => id ?? "null";
    }
}