// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol
{
    // ----------------------------------- sub task -----------------------------------
    public class References
    {
        /// Path to a <see cref="Ref{TKey,T}"/> field referencing an entity.
        /// These referenced entities are also loaded via the next <see cref="EntityStore.Sync"/> request.
        [Fri.Required]  public  string              selector; // e.g. ".items[*].article"
        [Fri.Required]  public  string              container;
                        public  string              keyName;
                        public  bool?               isIntKey;
                        public  List<References>    references;
    }
    
    // ----------------------------------- sub task result -----------------------------------
    public class ReferencesResult
    {
                        public  string                  error;
                        public  string                  container;
        [Fri.Required]  public  HashSet<JsonKey>        ids = new HashSet<JsonKey>(JsonKey.Equality);
                        public  List<ReferencesResult>  references;
    }
}