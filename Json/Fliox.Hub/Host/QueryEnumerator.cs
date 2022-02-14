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
        private     bool            detached;
        private     EntityContainer container;
        public      string          Cursor  { get; private set; }

        public abstract bool MoveNext();

        public void Reset() {
            throw new System.NotImplementedException();
        }

        public abstract JsonKey Current { get; }

        object IEnumerator.Current => throw new System.NotImplementedException();

        public void Dispose() {
            if (detached)
                return;
            DisposeEnumerator();
            var cursor = Cursor;
            if (cursor != null) {
                container.cursors.Remove(cursor);   
            }
        }
        
        // ---
        public      abstract bool               IsAsync             { get; }
        public      abstract JsonValue          CurrentValue        { get; }
        public      abstract Task<JsonValue>    CurrentValueAsync();
        protected   abstract void               DisposeEnumerator();

        public void Attach() {
            detached = false;
        }
        
        public void Detach() {
            detached = true;
        }
        
        public void Detach(string cursor, EntityContainer container) {
            detached        = true;
            Cursor          = cursor;
            this.container  = container;
        }
    }
}