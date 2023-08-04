// Copyright (c) Ullrich Praetz. All rights reserved.
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
        /// <summary>
        /// Return the <see cref="ReferencesResult.entities"/> referenced by the <see cref="References.selector"/> path
        /// of the given <paramref name="entities"/>
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
    }
}
