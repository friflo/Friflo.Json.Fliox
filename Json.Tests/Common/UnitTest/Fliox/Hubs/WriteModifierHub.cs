// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs
{
    public delegate JsonValue   WriteModifier (JsonValue value);
    
    public class WriteModifierHub : FlioxHub
    {
        private readonly    FlioxHub                                hub;
        private readonly    Dictionary<ShortString, WriteModifiers> writeModifiers  = new Dictionary<ShortString, WriteModifiers>(ShortString.Equality);
        private readonly    EntityProcessor                         processor       = new EntityProcessor();
        
        public WriteModifierHub(FlioxHub hub) : base(hub.database, hub.sharedEnv) {
            this.hub = hub;
        }

        public override void Dispose() {
            processor.Dispose();
        }

        public void ClearErrors() {
            foreach (var pair in writeModifiers) {
                var container = pair.Value;
                container.writes.Clear();
            }
        }

        public override ExecutionType InitSyncRequest(SyncRequest syncRequest) {
            foreach (var task in syncRequest.tasks.GetReadOnlySpan()) {
                switch (task) {
                    case CreateEntities createEntities:
                        if (writeModifiers.TryGetValue(createEntities.container, out var write)) {
                            write.ModifyWrites(createEntities.keyName, createEntities.entities);
                        }
                        break;
                    case UpsertEntities upsertEntities:
                        if (writeModifiers.TryGetValue(upsertEntities.container, out write)) {
                            write.ModifyWrites(upsertEntities.keyName, upsertEntities.entities);
                        }
                        break;
                }
            }
            return base.InitSyncRequest(syncRequest);
        }

        public WriteModifiers GetWriteModifiers(in ShortString container) {
            if (!writeModifiers.TryGetValue(container, out var writeModifier)) {
                writeModifier = new WriteModifiers(processor);
                writeModifiers.Add(container, writeModifier);
            }
            return writeModifier;
        }
    }
    
    public class WriteModifiers
    {
        public  readonly    Dictionary<string, WriteModifier>   writes    = new Dictionary<string, WriteModifier>();
        private readonly    EntityProcessor                     processor;
        
        internal WriteModifiers(EntityProcessor processor) {
            this.processor = processor;
        }
        
        internal void ModifyWrites(string keyName, List<JsonEntity> entities) {
            for (int n = 0; n < entities.Count; n++) {
                var entity = entities[n];
                if (!processor.GetEntityKey(entity.value, keyName, out JsonKey entityKey, out string error))
                    throw new InvalidOperationException($"Entity key error: {error}");
                var key = entityKey.AsString();
                if (writes.TryGetValue(key, out var modifier)) {
                    var modified    = modifier (entity.value);
                    entities[n]     = new JsonEntity(entity.key, modified);
                }
            }
        }
    }
}
