// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// A <see cref="FileDatabase"/> is used to store the entities / records of its containers as <b>JSON</b>
    /// files in the <b>file-system</b>.
    /// </summary>
    /// <remarks>
    /// Each database container / table is a sub folder of the path passed to the <see cref="FileDatabase"/> constructor.<br/>
    /// The intention of a <see cref="FileDatabase"/> is providing <b>out of the box</b> persistence without the need of
    /// installation or configuration of a third party database server like: SQLite, Postgres, ...<br/>
    /// This enables the following uses cases
    /// <list type="bullet">
    ///   <item>Creating <b>proof-of-concept</b> database applications without any third party dependencies</item>
    ///   <item>Suitable for <b>TDD</b> as test records are JSON files versioned via Git and providing access to their change history</item>
    ///   <item>Using a database <b>without configuration</b> by using a relative database path within a project</item>
    ///   <item>Viewing and editing database records as JSON files with <b>text editors</b> like VSCode, vi, web browsers, ...<br/></item>
    ///   <item>Using a <see cref="FileDatabase"/> as data source to <b>seed</b> other databases with <see cref="EntityDatabase.SeedDatabase"/></item>
    /// </list>
    /// In most uses cases a <see cref="FileDatabase"/> in not suitable for production as its read / write performance
    /// cannot compete with databases like: SQLite, Postgres, ... . <br/>
    /// <see cref="FileDatabase"/> has no third party dependencies.
    /// </remarks>
    public sealed class FileDatabase : EntityDatabase
    {
        private  readonly   string      databaseFolder;
        private  readonly   bool        pretty;
        public   override   string      StorageType => "file-system";
        
        public FileDatabase(string dbName, string databaseFolder, DatabaseService service = null, DbOpt opt = null, bool pretty = true)
            : base(dbName, service, opt)
        {
            this.pretty = pretty;
            this.databaseFolder = databaseFolder + "/";
            Directory.CreateDirectory(databaseFolder);
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new FileContainer(name, this, databaseFolder, pretty);
        }
        
        protected override Task<string[]> GetContainers() {
            var directories = Directory.GetDirectories(databaseFolder);
            var result = new string[directories.Length];
            for (int n = 0; n < directories.Length; n++) {
                result[n] = directories[n].Substring(databaseFolder.Length);
            }
            return Task.FromResult(result);
        }
    }
    
    public sealed class FileContainer : EntityContainer
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
        
        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            var entities = command.entities;
            List<EntityError> createErrors = null;
            await rwLock.AcquireWriterLock().ConfigureAwait(false);
            try {
                for (int n = 0; n < entities.Count; n++) {
                    var entity  = entities[n];
                    // if (payload.json == null)  continue; // TAG_ENTITY_NULL
                    var key     = entity.key;
                    var path = FilePath(key.AsString());
                    try {
                        await WriteText(path, entity.value, FileMode.CreateNew).ConfigureAwait(false);
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

        public override async Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, SyncContext syncContext) {
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
                        await WriteText(path, entity.value, FileMode.Create).ConfigureAwait(false);
                    } catch (Exception e) {
                        var error = CreateEntityError(EntityErrorType.WriteError, key, e);
                        AddEntityError(ref upsertErrors, key, error);
                    }
                }
            } finally {
                rwLock.ReleaseWriterLock();
            }
            return new UpsertEntitiesResult{ errors = upsertErrors };
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
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
                            var payload = await ReadText(filePath, buffer, syncContext.memoryBuffer).ConfigureAwait(false);
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
            var result = new ReadEntitiesResult{entities = entities};
            result.ValidateEntities(name, command.keyName, syncContext);
            return result;
        }

        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            if (!FindCursor(command.cursor, syncContext, out var fileEnumerator, out var error)) {
                return new QueryEntitiesResult { Error = error };
            }
            var keyValueEnum    = (FileQueryEnumerator)fileEnumerator ?? new FileQueryEnumerator(folder, syncContext.MemoryBuffer);
            var filterContext   = new EntityFilterContext(command, this, syncContext);
            var result          = new QueryEntitiesResult();
            try {
                while (keyValueEnum.MoveNext()) {
                    var key     = keyValueEnum.Current;
                    var value   = await keyValueEnum.CurrentValueAsync();
                    var filter  = filterContext.FilterEntity(key, value);
                    
                    if (filter == FilterEntityResult.FilterError)
                        return filterContext.QueryError(result);
                    if (filter == FilterEntityResult.ReachedLimit)
                        break;
                    if (filter == FilterEntityResult.ReachedMaxCount) {
                        result.cursor = StoreCursor(keyValueEnum, syncContext.User.userId);
                        break;
                    }
                }
                result.entities = filterContext.Result.ToArray();
                return result;
            } finally {
                filterContext.Dispose();
                keyValueEnum.Dispose();
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntities (AggregateEntities command, SyncContext syncContext) {
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
                    var result = await CountEntities(command, syncContext).ConfigureAwait(false);
                    return result;
            }
            return new AggregateEntitiesResult { Error = new CommandError($"aggregate {command.type} not implement") };
        }

        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            var keys = command.ids;
            List<EntityError> deleteErrors = null;
            await rwLock.AcquireWriterLock().ConfigureAwait(false);
            try {
                if (keys != null && keys.Count > 0) {
                    foreach (var key in keys) {
                        string path = FilePath(key.AsString());
                        try {
                            DeleteFile(path);
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
                        DeleteFile(fileName);
                    }
                }
            } finally {
                rwLock.ReleaseWriterLock();
            }
            var result = new DeleteEntitiesResult{ errors = deleteErrors };
            return result;
        }


        // -------------------------------------- helper methods --------------------------------------
        private static string GetHResultDetails(int result) {
            var lng = result & 0xffffffffL;
            switch (lng) {
                case 0x0000007B:   return "invalid file name";
                case 0x80070002:   return "file not found";
                case 0x80070050:   return "file already exists";
                case 0x80070052:   return "file cannot be created";
                case 0x80070570:   return "file corrupt";
            }
            return null;
        }
        
        private EntityError CreateEntityError (EntityErrorType type, in JsonKey key, Exception e) {
            var result = e.HResult;
            var details = GetHResultDetails(result);
            var sb = new StringBuilder();
            sb.Append($"HResult: 0x{result:X8}");
            if (details != null) {
                sb.Append(" - ");
                sb.Append(details);
            }
            var error = new EntityError(type, name, key, sb.ToString());
            return error;
        }
        
        /// <summary>
        /// Write with <see cref="FileShare.Read"/> as on a developer machine other processes like virus scanner or file watcher
        /// may access the file concurrently resulting in:
        /// IOException: The process cannot access the file 'path' because it is being used by another process
        /// </summary>
        private static async Task WriteText(string filePath, JsonValue json, FileMode fileMode) {
            using (var destStream = new FileStream(filePath, fileMode, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: false)) {
                await destStream.WriteAsync(json).ConfigureAwait(false);
                await destStream.FlushAsync().ConfigureAwait(false);
            }
        }
        
        internal static async Task<JsonValue> ReadText(string filePath, StreamBuffer buffer, MemoryBuffer memoryBuffer) {
            using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: false)) {
                var value = await EntityUtils.ReadToEndAsync(sourceStream, buffer).ConfigureAwait(false);
                return EntityUtils.CreateCopy(value, memoryBuffer);
            }
        }
        
        private static void DeleteFile(string filePath) {
            File.Delete(filePath);
        }
    }
    
    internal sealed class FileQueryEnumerator : QueryEnumerator
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly    string              folder; // keep there for debugging
        private readonly    int                 folderLen;
        private readonly    IEnumerator<string> enumerator;
        private readonly    StreamBuffer        buffer = new StreamBuffer();
        private readonly    MemoryBuffer        memoryBuffer;
            
        internal FileQueryEnumerator (string folder, MemoryBuffer memoryBuffer)
        {
            this.folder         = folder;
            folderLen           = folder.Length;
            this.memoryBuffer   = memoryBuffer; 
#if !UNITY_2020_1_OR_NEWER
            var options = new EnumerationOptions {
                MatchCasing             = MatchCasing.CaseSensitive,
                MatchType               = MatchType.Simple,
                RecurseSubdirectories   = false
            };
            enumerator = Directory.EnumerateFiles(folder, "*.json", options).GetEnumerator();
#else
            enumerator = Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly).GetEnumerator();
#endif
        }
            
        public override bool MoveNext() {
            return enumerator.MoveNext();
        }

        public override JsonKey Current { get {
            var fileName = enumerator.Current;
            var len = fileName.Length;
            var id = fileName.Substring(folderLen, len - folderLen - ".json".Length);
            return new JsonKey(id);
        } }
        
        protected override void DisposeEnumerator() {
            enumerator.Dispose();
        }
        
        public async       Task<JsonValue> CurrentValueAsync() { 
            var path    = enumerator.Current;
            return await FileContainer.ReadText(path, buffer, memoryBuffer).ConfigureAwait(false);
        }
    }
}
