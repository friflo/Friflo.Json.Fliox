// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Json.Burst;  // UnityExtension.TryAdd()
using Friflo.Json.Flow.Graph;

namespace Friflo.Json.EntityGraph.Database
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
        
        public override void CreateEntities(Dictionary<string, EntityValue> entities) {
            foreach (var entity in entities) {
                var path = FilePath(entity.Key);
                WriteText(path, entity.Value.value.json);
                // await File.WriteAllTextAsync(path, entity.value);
            }
        }

        public override void UpdateEntities(Dictionary<string, EntityValue> entities) {
            throw new NotImplementedException();
        }

        public override Dictionary<string, EntityValue> ReadEntities(ICollection<string> ids) {
            var result = new Dictionary<string,EntityValue>();
            foreach (var id in ids) {
                var filePath = FilePath(id);
                string payload = null;
                if (File.Exists(filePath)) {
                    payload = ReadText(filePath);
                    // payload = await File.ReadAllTextAsync(filePath);
                }
                var entry = new EntityValue(payload);
                result.TryAdd(id, entry);
            }
            return result;
        }

        public override Dictionary<string, EntityValue> QueryEntities(FilterOperation filter) {
            var result = new Dictionary<string, EntityValue>();
            return result;
        }
        
        private static void WriteText(string filePath, string text)
        {
            byte[] encodedText = Encoding.UTF8.GetBytes(text);
            using (var sourceStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: false))
            {
                sourceStream.Write(encodedText, 0, encodedText.Length);
            }
        }
        
        private static string ReadText(string filePath)
        {
            using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: false))
            {
                var sb = new StringBuilder();
                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = sourceStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string text = Encoding.UTF8.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }
                return sb.ToString();
            }
        }
    }
}
