// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host
{
    public abstract class QueryEnumerator : IEnumerator<JsonKey>
    {
        internal   bool    detached;
        
        public abstract bool MoveNext();

        public void Reset() {
            throw new System.NotImplementedException();
        }

        public abstract JsonKey Current { get; }

        object IEnumerator.Current => throw new System.NotImplementedException();

        public abstract void Dispose();
        
        // ---
        public abstract bool            IsAsync             { get; }
        public abstract JsonValue       CurrentValue        { get; }
        public abstract Task<JsonValue> CurrentValueAsync();
        
        public void Detach() {
            detached = true;
        }
    }
}