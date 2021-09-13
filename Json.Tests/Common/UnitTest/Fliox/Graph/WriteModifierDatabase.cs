// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph
{
    public delegate JsonValue   WriteModifier (JsonValue value);
    public delegate EntityPatch PatchModifier (EntityPatch patch);
    
    public class WriteModifierDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  local;
        private readonly    Dictionary<string, WriteModifiers>  writeModifiers  = new Dictionary<string, WriteModifiers>();
        private readonly    Dictionary<string, PatchModifiers>  patchModifiers  = new Dictionary<string, PatchModifiers>();
        private readonly    EntityValidator                     validator       = new EntityValidator();
        
        public WriteModifierDatabase(EntityDatabase local) {
            this.local = local;
        }

        public override void Dispose() {
            validator.Dispose();
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
                        if (writeModifiers.TryGetValue(createEntities.container, out var write)) {
                            write.ModifyWrites(createEntities.keyName, createEntities.entities);
                        }
                        break;
                    case UpsertEntities updateEntities:
                        if (writeModifiers.TryGetValue(updateEntities.container, out write)) {
                            write.ModifyWrites(updateEntities.keyName, updateEntities.entities);
                        }
                        break;
                    case PatchEntities patchEntities:
                        if (patchModifiers.TryGetValue(patchEntities.container, out var patch)) {
                            patch.ModifyPatches(patchEntities.patches);
                        }
                        break;
                }
            }
            return await local.ExecuteSync(syncRequest, messageContext);
        }

        public WriteModifiers GetWriteModifiers<TEntity>() where TEntity : class {
            var name = typeof(TEntity).Name;
            if (!writeModifiers.TryGetValue(name, out var writeModifier)) {
                writeModifier = new WriteModifiers(validator);
                writeModifiers.Add(name, writeModifier);
            }
            return writeModifier;
        }
        
        public PatchModifiers GetPatchModifiers<TEntity>() where TEntity : class {
            var name = typeof(TEntity).Name;
            if (!patchModifiers.TryGetValue(name, out var patchModifier)) {
                patchModifier = new PatchModifiers();
                patchModifiers.Add(name, patchModifier);
            }
            return patchModifier;
        }
    }
    
    public class WriteModifiers
    {
        public  readonly    Dictionary<string, WriteModifier>   writes    = new Dictionary<string, WriteModifier>();
        private readonly    EntityValidator                     validator;
        
        internal WriteModifiers(EntityValidator validator) {
            this.validator = validator;
        }
        
        internal void ModifyWrites(string keyName, List<JsonValue> entities) {
            for (int n = 0; n < entities.Count; n++) {
                var entity = entities[n];
                var json = entity.json;
                if (!validator.GetEntityKey(json, keyName, out JsonKey entityKey, out string error))
                    throw new InvalidOperationException($"Entity key error: {error}");
                var key = entityKey.AsString();
                if (writes.TryGetValue(key, out var modifier)) {
                    var modified    = modifier (entity);
                    entities[n] = modified;
                }
            }
        }
    }
    
    public class PatchModifiers
    {
        public  readonly    Dictionary<string, PatchModifier>    patches    = new Dictionary<string, PatchModifier>();
        
        internal void ModifyPatches(Dictionary<JsonKey, EntityPatch> entityPatches) {
            var modifications = new Dictionary<string, EntityPatch>();
            foreach (var pair in entityPatches) {
                var key = pair.Key.AsString();
                if (patches.TryGetValue(key, out var modifier)) {
                    EntityPatch value       = pair.Value;
                    EntityPatch modified    = modifier (value);
                    modifications.Add(key, modified);
                }
            }
            foreach (var pair in modifications) {
                var         key     = new JsonKey(pair.Key);
                EntityPatch value   = pair.Value;
                entityPatches[key] = value;
            }
        }
    }
}
