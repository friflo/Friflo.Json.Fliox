// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public delegate EntityValue Modifier (EntityValue processor);
    
    public class WriteModifierDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  local;
        private readonly    Dictionary<string, WriteModifier>   writeModifiers  = new Dictionary<string, WriteModifier>();
        
        public WriteModifierDatabase(EntityDatabase local) {
            this.local = local;
        }
        
        public void ClearErrors() {
            foreach (var pair in writeModifiers) {
                var container = pair.Value;
                container.writes.Clear();
            }
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return local.CreateContainer(name, database);
        }
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            foreach (var task in syncRequest.tasks) {
                switch (task) {
                    case CreateEntities createEntities:
                        if (writeModifiers.TryGetValue(createEntities.container, out var writesModifier)) {
                            writesModifier.ModifyWrites(createEntities.entities, writesModifier.writes);
                        }
                        break;
                    case UpdateEntities updateEntities:
                        if (writeModifiers.TryGetValue(updateEntities.container, out writesModifier)) {
                            writesModifier.ModifyWrites(updateEntities.entities, writesModifier.writes);
                        }
                        break;
                }
            }
            return await local.ExecuteSync(syncRequest, messageContext);
        }

        public WriteModifier GetWriteModifier<TEntity>() where TEntity : Entity {
            var name = typeof(TEntity).Name;
            if (!writeModifiers.TryGetValue(name, out var writeModifier)) {
                writeModifier = new WriteModifier();
                writeModifiers.Add(name, writeModifier);
            }
            return writeModifier;
        }
    }
    
    public class WriteModifier
    {
        public  readonly    Dictionary<string, Modifier>    writes    = new Dictionary<string, Modifier>();
        
        internal void ModifyWrites(Dictionary<string, EntityValue> entities, Dictionary<string, Modifier> creates) {
            var modifications = new Dictionary<string, EntityValue>();
            foreach (var pair in entities) {
                var key = pair.Key;
                if (creates.TryGetValue(key, out var modifier)) {
                    var value       = pair.Value;
                    var modified    = modifier (value);
                    modifications.Add(key, modified);
                }
            }
            foreach (var pair in modifications) {
                var key     = pair.Key;
                var value   = pair.Value;
                entities[key] = value;
            }
        }
    }
}
