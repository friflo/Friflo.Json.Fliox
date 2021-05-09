// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Database
{
    public class FileDatabase : EntityDatabase
    {
        private readonly    string  databaseFolder;
        private readonly    bool    pretty;

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
        private readonly    string          folder;

        public  override    bool            Pretty      { get; }
        public  override    SyncContext     SyncContext { get; }


        public FileContainer(string name, EntityDatabase database, string folder, bool pretty) : base (name, database) {
            this.Pretty = pretty;
            SyncContext = new SyncContext();
            this.folder = folder + "/";
            Directory.CreateDirectory(folder);
        }

        public override void Dispose() {
            SyncContext.Dispose();
        }

        private string FilePath(string key) {
            return folder + key + ".json";
        }
        
        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command) {
            var entities = command.entities;
            var result = new CreateEntitiesResult{createErrors = new Dictionary<string, EntityError>()};
            foreach (var entityPair in entities) {
                string      key     = entityPair.Key;
                EntityValue payload = entityPair.Value;
                var path = FilePath(key);
                try {
                    await WriteText(path, payload.Json);
                } catch (Exception e) {
                    var error = new EntityError(EntityErrorType.WriteError, name, key, e.Message);
                    result.createErrors.Add(key, error);
                }
            }
            return result;
        }

        public override async Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command) {
            var entities = command.entities;
            foreach (var entityPair in entities) {
                string      key     = entityPair.Key;
                EntityValue payload = entityPair.Value;
                var path = FilePath(key);
                await WriteText(path, payload.Json);
                // await File.WriteAllTextAsync(path, payload);
            }
            return new UpdateEntitiesResult();
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command) {
            var keys        = command.ids;
            var entities    = new Dictionary<string, EntityValue>(keys.Count);
            foreach (var key in keys) {
                var filePath = FilePath(key);
                EntityValue entry;
                if (File.Exists(filePath)) {
                    try {
                        var payload = await ReadText(filePath);
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
            return new ReadEntitiesResult{entities = entities};
        }

        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command) {
            var keys            = GetIds(folder);
            var readIds         = new ReadEntities {ids = keys};
            var readEntities    = await ReadEntities(readIds);
            var jsonFilter      = new JsonFilter(command.filter); // filter can be reused
            var result          = new Dictionary<string, EntityValue>();
            foreach (var entityPair in readEntities.entities) {
                var key     = entityPair.Key;
                var payload = entityPair.Value.Json;
                if (SyncContext.jsonEvaluator.Filter(payload, jsonFilter)) {
                    var entry = new EntityValue(payload);
                    result.Add(key, entry);
                }
            }
            return new QueryEntitiesResult{entities = result};
        }

        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command) {
            var keys = command.ids;
            foreach (var key in keys) {
                string path = FilePath(key);
                DeleteFile(path);
            }
            var result = new DeleteEntitiesResult();
            return Task.FromResult(result);
        }
        
        
        // -------------------------------------- helper methods -------------------------------------- 
        private static HashSet<string> GetIds(string folder) {
            string[] fileNames = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
#if UNITY_5_3_OR_NEWER
            var ids = new HashSet<string>();
#else
            var ids = new HashSet<string>(fileNames.Length);
#endif
            for (int n = 0; n < fileNames.Length; n++) {
                var fileName = fileNames[n];
                var len = fileName.Length;
                var id = fileName.Substring(folder.Length, len - folder.Length - ".json".Length);
                ids.Add(id);
            }
            return ids;
        }
        
        private static async Task WriteText(string filePath, string text) {
            byte[] encodedText = Encoding.UTF8.GetBytes(text);
            using (var destStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: false)) {
                await destStream.WriteAsync(encodedText, 0, encodedText.Length);
            }
        }
        
        private static async Task<string> ReadText(string filePath) {
            using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: false)) {
                var sb = new StringBuilder();
                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0) {
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
