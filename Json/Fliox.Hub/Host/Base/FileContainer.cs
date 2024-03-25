// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Fliox.Utils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    internal sealed class FileContainer : EntityContainer
    {
        private  readonly   string                  folder;
        private  readonly   AsyncReaderWriterLock   rwLock;

        public   override   bool                    Pretty      { get; }


        public FileContainer(string name, EntityDatabase database, string databaseFolder, bool pretty)
            : base (name, database)
        {
            this.Pretty = pretty;
            this.folder = databaseFolder + instanceName + "/";
            rwLock      = new AsyncReaderWriterLock();
            Directory.CreateDirectory(folder);
        }

        public override void Dispose() {
            rwLock.Dispose();
            base.Dispose();
        }

        private string FilePath(string key) {
            return folder + key + ".json";
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var entities = command.entities;
            List<EntityError> createErrors = null;
            await rwLock.AcquireWriterLock().ConfigureAwait(false);
            try {
                for (int n = 0; n < entities.Count; n++) {
                    var entity  = entities[n];
                    // if (payload.json == null)  continue; // TAG_ENTITY_NULL
                    var key     = entity.key;
                    var path    = FilePath(key.AsString());
                    // use File.Exists() to avoid throwing exception when calling new FileStream()
                    if (File.Exists(path)) {
                        var error = new EntityError(EntityErrorType.WriteError, nameShort, key, "file already exist");
                        AddEntityError(ref createErrors, key, error);
                        continue;
                    }
                    try {
                        await FileUtils.WriteText(path, entity.value, FileMode.CreateNew).ConfigureAwait(false);
                    } catch (Exception e) {
                        var error = CreateEntityError(EntityErrorType.WriteError, key, e);
                        AddEntityError(ref createErrors, key, error);
                    }
                }
            } finally {
                rwLock.ReleaseWriterLock();
            }
            return new CreateEntitiesResult{ errors = createErrors };
        }

        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var entities = command.entities;
            List<EntityError> upsertErrors = null;
            await rwLock.AcquireWriterLock().ConfigureAwait(false);
            try {
                for (int n = 0; n < entities.Count; n++) {
                    var entity = entities[n];
                    // if (entity.json == null)  continue; // TAG_ENTITY_NULL
                    var key     = entity.key;
                    var path    = FilePath(key.AsString());
                    try {
                        await FileUtils.WriteText(path, entity.value, FileMode.Create).ConfigureAwait(false);
                    } catch (Exception e) {
                        var error = CreateEntityError(EntityErrorType.WriteError, key, e);
                        AddEntityError(ref upsertErrors, key, error);
                    }
                }
            } finally {
                rwLock.ReleaseWriterLock();
            }
            return UpsertEntitiesResult.Create(syncContext, upsertErrors);
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var keys        = command.ids;
            var entities    = new EntityValue[keys.Count];
            int index       = 0;
            var buffer      = new StreamBuffer();
            await rwLock.AcquireReaderLock().ConfigureAwait(false);
            try {
                foreach (var key in keys) {
                    var filePath = FilePath(key.AsString());
                    EntityValue entry;
                    if (File.Exists(filePath)) {
                        try {
                            var payload = await FileUtils.ReadText(filePath, buffer, syncContext.MemoryBuffer).ConfigureAwait(false);
                            entry = new EntityValue(key, payload);
                        } catch (Exception e) {
                            var error = CreateEntityError(EntityErrorType.ReadError, key, e);
                            entry = new EntityValue(key, error);
                        }
                    } else {
                        entry = new EntityValue(key);
                    }
                    entities[index++] = entry;
                }
            } finally {
                rwLock.ReleaseReaderLock();
            }
            var result = new ReadEntitiesResult{ entities = new Entities(entities) };
            result.ValidateEntities(nameShort, command.keyName, syncContext);
            return result;
        }

        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            if (!FindCursor(command.cursor, syncContext, out var fileEnumerator, out var error)) {
                return new QueryEntitiesResult { Error = error };
            }
            var keyValueEnum    = (FileQueryEnumerator)fileEnumerator ?? new FileQueryEnumerator(folder, syncContext.MemoryBuffer);
            var filterContext   = new EntityFilterContext(command, this, syncContext);
            var result          = new QueryEntitiesResult();
            try {
                while (keyValueEnum.MoveNext()) {
                    var key     = keyValueEnum.Current;
                    var value   = await keyValueEnum.CurrentValueAsync().ConfigureAwait(false);
                    var filter  = filterContext.FilterEntity(key, value);
                    
                    if (filter == FilterEntityResult.FilterError)
                        return filterContext.QueryError(result);
                    if (filter == FilterEntityResult.ReachedLimit)
                        break;
                    if (filter == FilterEntityResult.ReachedMaxCount) {
                        result.cursor = StoreCursor(keyValueEnum, syncContext.User);
                        break;
                    }
                }
                result.entities = new Entities(filterContext.Result);
                return result;
            } finally {
                filterContext.Dispose();
                keyValueEnum.Dispose();
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var filter = command.GetFilter();
            switch (command.type) {
                case AggregateType.count:
                    // count all?
                    if (filter.IsTrue) {
                        var keyValueEnum = new FileQueryEnumerator (folder, syncContext.MemoryBuffer);
                        try {
                            var count = 0;
                            while (keyValueEnum.MoveNext()) { count++; }
                            return new AggregateEntitiesResult { container = command.container, value = count };
                        }
                        finally {
                            keyValueEnum.Dispose();
                        }
                    }
                    var result = await CountEntitiesAsync(command, syncContext).ConfigureAwait(false);
                    return result;
            }
            return new AggregateEntitiesResult { Error = new TaskExecuteError($"aggregate {command.type} not implement") };
        }

        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var keys = command.ids;
            List<EntityError> deleteErrors = null;
            await rwLock.AcquireWriterLock().ConfigureAwait(false);
            try {
                if (keys != null && keys.Count > 0) {
                    foreach (var key in keys) {
                        string path = FilePath(key.AsString());
                        try {
                            FileUtils.DeleteFile(path);
                        } catch (Exception e) {
                            var error = CreateEntityError(EntityErrorType.DeleteError, key, e);
                            AddEntityError(ref deleteErrors, key, error);
                        }
                    }
                }
                var all = command.all;
                if (all != null && all.Value) {
                    string[] fileNames = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
                    foreach (var fileName in fileNames) {
                        FileUtils.DeleteFile(fileName);
                    }
                }
            } finally {
                rwLock.ReleaseWriterLock();
            }
            var result = new DeleteEntitiesResult{ errors = deleteErrors };
            return result;
        }
        
        private EntityError CreateEntityError (EntityErrorType type, in JsonKey key, Exception e) {
            var result = e.HResult;
            var details = FileUtils.GetHResultDetails(result);
            var sb = new StringBuilder();
            sb.Append($"HResult: 0x{result:X8}");
            if (details != null) {
                sb.Append(" - ");
                sb.Append(details);
            }
            var error = new EntityError(type, nameShort, key, sb.ToString());
            return error;
        }
    }
}