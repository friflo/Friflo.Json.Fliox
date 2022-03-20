// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    // ----------------------------------- sub task -----------------------------------
    /// <summary>
    /// <see cref="References"/> are used to return entities referenced by fields of entities returned by read and query tasks.<br/>
    /// <see cref="References"/> can be nested to return referenced entities of referenced entities.
    /// </summary>
    public sealed class References
    {
        /// <remarks>
        /// Path to a <see cref="Client.Ref{TKey,T}"/> field referencing an entity.
        /// These referenced entities are also loaded via the next <see cref="FlioxClient.SyncTasks"/> request.
        /// </remarks>
        /// <summary>the field path used as a reference to an entity in the specified <see cref="container"/></summary>
        [Fri.Required]  public  string              selector; // e.g. ".items[*].article"
        /// <summary>the <see cref="container"/> storing the entities referenced by the specified <see cref="selector"/></summary>
        [Fri.Required]  public  string              container;
                        public  string              keyName;
                        public  bool?               isIntKey;
                        public  List<References>    references;
    }
    
    // ----------------------------------- sub task result -----------------------------------
    public sealed class ReferencesResult
    {
                        public  string                  error;
        [DebugInfo]     public  string                  container;
        /// <summary>number of <see cref="ids"/> - not utilized by Protocol</summary>
        [DebugInfo]     public  int?                    count;
        [Fri.Required]  public  HashSet<JsonKey>        ids = new HashSet<JsonKey>(JsonKey.Equality);
                        public  List<ReferencesResult>  references;
    }
}