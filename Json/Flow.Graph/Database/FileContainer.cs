// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
    public class FileDatabase : EntityDatabase
    {
        private  readonly   string  databaseFolder;
        private  readonly   bool    pretty;

        public FileDatabase(string databaseFolder, bool pretty = true) {
            this.pretty = pretty;
            this.databaseFolder = databaseFolder + "/";
            Directory.CreateDirectory(databaseFolder);
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new FileContainer(name, this, databaseFolder + name, pretty);
        }
    }
    
    public class FileContainer : EntityContainer
    {
        private  readonly   string                  folder;
        private  readonly   AsyncReaderWriterLock   rwLock;

        public   override   bool                    Pretty      { get; }


        public FileContainer(string name, EntityDatabase database, string folder, bool pretty) : base (name, database) {
            this.Pretty = pretty;
            this.folder = folder + "/";
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
        
        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            var entities = command.entities;
            Dictionary<string, EntityError> createErrors = null;
            await rwLock.AcquireWriterLock().ConfigureAwait(false);
            try {
                foreach (var entityPair in entities) {
                    string      key     = entityPair.Key;
                    EntityValue payload = entityPair.Value;
                    var path = FilePath(key);
                    try {
                        await WriteText(path, payload.Json).ConfigureAwait(false);
                    } catch (Exception e) {
                        var error = new EntityError(EntityErrorType.WriteError, name, key, e.Message);
                        AddEntityError(ref createErrors, key, error);
                    }
                }
            } finally {
                rwLock.ReleaseWriterLock();
            }
            return new CreateEntitiesResult{createErrors = createErrors};
        }

        public override async Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command, MessageContext messageContext) {
            var entities = command.entities;
            Dictionary<string, EntityError> updateErrors = null;
            await rwLock.AcquireWriterLock().ConfigureAwait(false);
            try {
                foreach (var entityPair in entities) {
                    string      key     = entityPair.Key;
                    EntityValue payload = entityPair.Value;
                    var path = FilePath(key);
                    try {
                        await WriteText(path, payload.Json).ConfigureAwait(false);
                    } catch (Exception e) {
                        var error = new EntityError(EntityErrorType.WriteError, name, key, e.Message);
                        AddEntityError(ref updateErrors, key, error);
                    }
                }
            } finally {
                rwLock.ReleaseWriterLock();
            }
            return new UpdateEntitiesResult{updateErrors = updateErrors};
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, MessageContext messageContext) {
            var keys        = command.ids;
            var entities    = new Dictionary<string, EntityValue>(keys.Count);
            await rwLock.AcquireReaderLock().ConfigureAwait(false);
            try {
                foreach (var key in keys) {
                    var filePath = FilePath(key);
                    EntityValue entry;
                    if (File.Exists(filePath)) {
                        try {
                            var payload = await ReadText(filePath).ConfigureAwait(false);
                            entry = new EntityValue(payload);
                        } catch (Exception e) {
                            var error = new EntityError(EntityErrorType.ReadError, name, key, e.Message);
                            entry = new EntityValue(error);
                        }
                    } else {
                        entry = new EntityValue();
                    }
                    entities.TryAdd(key, entry);
                }
            } finally {
                rwLock.ReleaseReaderLock();
            }
            var result = new ReadEntitiesResult{entities = entities};
            result.ValidateEntities(name, messageContext);
            return result;
        }

        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            var ids     = GetIds(folder);
            var result  = await FilterEntities(command, ids, messageContext).ConfigureAwait(false);
            return result;
        }

        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            var keys = command.ids;
            Dictionary<string, EntityError> deleteErrors = null;
            await rwLock.AcquireWriterLock().ConfigureAwait(false);
            try {
                foreach (var key in keys) {
                    string path = FilePath(key);
                    try {
                        DeleteFile(path);
                    } catch (Exception e) {
                        var error = new EntityError(EntityErrorType.DeleteError, name, key, e.Message);
                        AddEntityError(ref deleteErrors, key, error);
                    }
                }
            } finally {
                rwLock.ReleaseWriterLock();
            }
            var result = new DeleteEntitiesResult{deleteErrors = deleteErrors};
            return result;
        }
        
        
        // -------------------------------------- helper methods -------------------------------------- 
        private static HashSet<string> GetIds(string folder) {
            string[] fileNames = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
            var ids = Helper.CreateHashSet<string>(fileNames.Length);
            for (int n = 0; n < fileNames.Length; n++) {
                var fileName = fileNames[n];
                var len = fileName.Length;
                var id = fileName.Substring(folder.Length, len - folder.Length - ".json".Length);
                ids.Add(id);
            }
            return ids;
        }
        
        /// <summary>
        /// Write with <see cref="FileShare.Read"/> as on a developer machine other processes like virus scanner or file watcher
        /// may access the file concurrently resulting in:
        /// IOException: The process cannot access the file 'path' because it is being used by another process
        /// </summary>
        private static async Task WriteText(string filePath, string text) {
            byte[] encodedText = Encoding.UTF8.GetBytes(text);
            using (var destStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: false)) {
                await destStream.WriteAsync(encodedText, 0, encodedText.Length).ConfigureAwait(false);
            }
        }
        
        private static async Task<string> ReadText(string filePath) {
            using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: false)) {
                var sb = new StringBuilder();
                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0) {
                    string text = Encoding.UTF8.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }
                return sb.ToString();
            }
        }
        
        private static void DeleteFile(string filePath) {
            File.Delete(filePath);
        }
    }
}
