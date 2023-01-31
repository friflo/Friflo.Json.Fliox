// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.Client;

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
        /// Path to a field used as entity reference (secondary key). This field should be annotated with <see cref="RelationAttribute"/>
        /// The referenced entities are also loaded via the next <see cref="FlioxClient.SyncTasks"/> request.
        /// </remarks>
        /// <summary>the field path used as a reference to an entity in the specified <see cref="container"/></summary>
        [Required]  public  string              selector; // e.g. ".items[*].article"
        /// <summary>the <see cref="container"/> storing the entities referenced by the specified <see cref="selector"/></summary>
        [Serialize                            ("cont")]
        [Required]  public  ShortString         container;
                    public  string              keyName;
                    public  bool?               isIntKey;
                    public  List<References>    references;
    }
    
    // ----------------------------------- sub task result -----------------------------------
    public sealed class ReferencesResult
    {
                    public  string                  error;
        /// <summary>container name - not utilized by Protocol</summary>
        [Serialize                                ("cont")]
        [DebugInfo] public  ShortString             container;
        /// <summary>number of <see cref="ids"/> - not utilized by Protocol</summary>
        [DebugInfo] public  int?                    len;
        [Required]  public  List<JsonKey>           ids;
                    public  List<ReferencesResult>  references;
    }
}