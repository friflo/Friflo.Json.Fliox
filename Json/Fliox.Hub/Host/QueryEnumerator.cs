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
        /// A non detached enumerator free resources of its internal enumerator when calling <see cref="Dispose"/> 
        private     bool            detached;
        private     EntityContainer container;
        /// Ensure a stored cursor can be accessed only by the user created this cursor
        public      JsonKey         UserId  { get; private set; }
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
        
        public void Detach(string cursor, EntityContainer container, in JsonKey userId) {
            detached        = true;
            Cursor          = cursor;
            UserId          = userId;
            this.container  = container;
        }
    }
}