// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Protocol.Models
{
    // ----------------------------------- sub task -----------------------------------
    public sealed class References
    {
        /// Path to a <see cref="Client.Ref{TKey,T}"/> field referencing an entity.
        /// These referenced entities are also loaded via the next <see cref="FlioxClient.SyncTasks"/> request.
        [Fri.Required]  public  string              selector; // e.g. ".items[*].article"
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
        /// <summary> Is used only to show the number of <see cref="ids"/> in a serialized protocol message
        /// to avoid counting them by hand when debugging.
        /// It is not used by the library as it is redundant information. </summary>
        [DebugInfo]     public  int?                    count;
        [Fri.Required]  public  HashSet<JsonKey>        ids = new HashSet<JsonKey>(JsonKey.Equality);
                        public  List<ReferencesResult>  references;
    }
}