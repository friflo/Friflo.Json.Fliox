// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public abstract class WriteTask<T> : SyncTask where T : class {
        internal readonly   List<KeyEntity<T>>  entities = new List<KeyEntity<T>>();
        
        [DebuggerBrowsable(Never)]
        internal            TaskState           state;
        internal override   TaskState           State       => state;

        internal WriteTask(Set entitySet) : base(entitySet) { }
    }
    
    internal readonly struct KeyEntity<T>  where T : class 
    {
        internal readonly  JsonKey  key;
        internal readonly  T        value;

        public   override   string  ToString() => key.AsString();

        internal KeyEntity(in JsonKey key, T value) {
            this.key    = key;
            this.value  = value;
        }
    }
}