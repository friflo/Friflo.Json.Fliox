// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- sub task -----------------------------------
    public class References
    {
        /// Path to a <see cref="Ref{T}"/> field referencing an entity.
        /// These referenced entities are also loaded via the next <see cref="EntityStore.Sync"/> request.
        [Fri.Property(Required = true)]
        public  string                  selector; // e.g. ".items[*].article"
        [Fri.Property(Required = true)]
        public  string                  container;
        public  List<References>        references;
    }
    
    // ----------------------------------- sub task result -----------------------------------
    public class ReferencesResult
    {
        public  string                  error;
        public  string                  container;
        [Fri.Property(Required = true)]
        public  HashSet<string>         ids;
        public  List<ReferencesResult>  references;
    }
}