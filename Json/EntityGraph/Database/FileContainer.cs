// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Friflo.Json.EntityGraph.Database
{
    public class FileDatabase : EntityDatabase
    {
        private readonly string databaseFolder;

        public FileDatabase(string databaseFolder) {
            this.databaseFolder = databaseFolder + "/";
            Directory.CreateDirectory(databaseFolder);
        }

        protected override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new FileContainer(name, database, databaseFolder + name);
        }
    }
    
    public class FileContainer : EntityContainer
    {
        private readonly string folder;
        
        public FileContainer(string name, EntityDatabase database, string folder) : base (name, database) {
            this.folder = folder + "/";
            Directory.CreateDirectory(folder);
        }

        private string FilePath(string key) {
            return folder + key + ".json";
        }
        

#pragma warning disable 1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await TaskEx.Run(...)' to do CPU-bound work on a background thread
        public override async Task CreateEntities(ICollection<KeyValue> entities) {
            foreach (var entity in entities) {
                var path = FilePath(entity.key);
                await WriteTextAsync(path, entity.value);
                // await File.WriteAllTextAsync(path, entity.value);
            }
        }

        public override async Task UpdateEntities(ICollection<KeyValue> entities) {
            throw new NotImplementedException();
        }

        public override async Task<ICollection<KeyValue>> ReadEntities(ICollection<string> ids) {
            var result = new List<KeyValue>();
            foreach (var id in ids) {
                var filePath = FilePath(id);
                string payload = null;
                if (File.Exists(filePath)) {
                    payload = await ReadTextAsync(filePath);
                    // payload = await File.ReadAllTextAsync(filePath);
                }
                var entry = new KeyValue {
                    key = id,
                    value = payload
                };
                result.Add(entry);
            }
            return result;
        }
#pragma warning restore 1998
        
        private static async Task WriteTextAsync(string filePath, string text)
        {
            byte[] encodedText = Encoding.UTF8.GetBytes(text);
            using (var sourceStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            }
        }
        
        private static async Task<string> ReadTextAsync(string filePath)
        {
            using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true))
            {
                var sb = new StringBuilder();
                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string text = Encoding.UTF8.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }
                return sb.ToString();
            }
        }
    }
}
