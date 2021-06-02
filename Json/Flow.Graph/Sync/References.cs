// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Friflo.Json.Flow.Graph;

namespace Friflo.Json.Flow.Sync
{
    public class References
    {
        /// Path to a <see cref="Ref{T}"/> field referencing an <see cref="Entity"/>.
        /// These referenced entities are also loaded via the next <see cref="EntityStore.Sync"/> request.
        public  string                  selector; // e.g. ".items[*].article"
        public  string                  container;
        public  List<References>        references;
    }
    
    public class ReferencesResult
    {
        public  string                  error;
        public  string                  container;
        public  HashSet<string>         ids;
        public  List<ReferencesResult>  references;
    }
}