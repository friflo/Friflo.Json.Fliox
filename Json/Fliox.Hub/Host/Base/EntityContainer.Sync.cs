// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static Friflo.Json.Fliox.Hub.Host.EntityContainerUtils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public partial class EntityContainer
    {
        // ----------------------------------------- sync / async -----------------------------------------
        
        /// <summary>Apply the given <paramref name="mergeEntities"/> to the container entities</summary>
        /// <remarks>
        /// Default implementation to apply patches to entities.
        /// The implementation perform three steps:
        /// 1. Read entities to be patches from a database
        /// 2. Apply merge patches
        /// 3. Write back the merged entities
        ///
        /// If the used database has integrated support for merging (patching) JSON its <see cref="EntityContainer"/>
        /// implementation can override this method to replace two database requests by one.<br/>
        /// <br/>
        /// Counterpart of <see cref="MergeEntitiesAsync"/>
        /// </remarks>
        public virtual MergeEntitiesResult MergeEntities (MergeEntities mergeEntities, SyncContext syncContext)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Default implementation. Performs a full table scan! Act as reference and is okay for small data sets.<br/>
        /// Counterpart <see cref="CountEntitiesAsync"/>
        /// </summary>
        protected AggregateEntitiesResult CountEntities (AggregateEntities command, SyncContext syncContext)
        {
            var query = new QueryEntities (command.container, command.filter, command.filterTree, command.filterContext);
            var queryResult = QueryEntities(query, syncContext);
            
            var queryError = queryResult.Error; 
            if (queryError != null) {
                return new AggregateEntitiesResult { Error = queryError };
            }
            return new AggregateEntitiesResult { container = command.container, value = queryResult.entities.Length };
        }
        
        /// <summary>
        /// Return the <see cref="ReferencesResult.entities"/> referenced by the <see cref="References.selector"/> path
        /// of the given <paramref name="entities"/>.<br/>
        ///
        /// Counterpart <see cref="ReadReferencesAsync"/>
        /// </summary>
        internal ReadReferencesResult ReadReferences(
            List<References>    references,
            Entities            entities,
            ShortString         container,
            string              selectorPath,
            SyncContext         syncContext)
        {
            var referenceResults = GetReferences(references, entities, container, syncContext);
            
            for (int n = 0; n < references.Count; n++) {
                var reference       = references[n];
                var refCont         = database.GetOrCreateContainer(reference.container);
                var referenceResult = referenceResults[n];
                var foreignKeys     = referenceResult.foreignKeys;
                if (foreignKeys.Count == 0) {
                    referenceResult.entities = new Entities(Array.Empty<EntityValue>());
                    continue;
                }
                var readRefIds  = new ReadEntities { ids = foreignKeys, keyName = reference.keyName, isIntKey = reference.isIntKey};
                
                // read all referenced entities with a single read command.
                var refEntities = refCont.ReadEntities(readRefIds, syncContext);
                
                if (!ProcessRefEntities(reference, referenceResult, container, selectorPath, refEntities, out var subEntities, out var subPath)) {
                    continue;
                }
                var refResult = ReadReferences(reference.references, subEntities, reference.container, subPath, syncContext);
                // returned refResult.references is always set. Each references[] item contain either a result or an error.
                referenceResult.references = refResult.references;
            }
            return new ReadReferencesResult { references = referenceResults };
        }
        // --------------------------------------- end: sync / async ---------------------------------------
    }
}
