// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.EntityGraph.Database
{
    public class FileDatabase : EntityDatabase
    {
        private readonly    string          databaseFolder;


        public FileDatabase(string databaseFolder, bool pretty = false) {
            this.databaseFolder = databaseFolder + "/";
            Directory.CreateDirectory(databaseFolder);
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new FileContainer(name, (FileDatabase)database, databaseFolder + name);
        }
    }
    
    public class FileContainer : EntityContainer
    {
        private readonly string         folder;
        private readonly FileDatabase   fileDatabase;
        
        public FileContainer(string name, FileDatabase database, string folder) : base (name, database) {
            this.fileDatabase = database;
            this.folder = folder + "/";
            Directory.CreateDirectory(folder);
        }

        private string FilePath(string key) {
            return folder + key + ".json";
        }
        
        public override void CreateEntities(ICollection<KeyValue> entities) {
            foreach (var entity in entities) {
                var path = FilePath(entity.key);
                WriteText(path, entity.value.json);
                // await File.WriteAllTextAsync(path, entity.value);
            }
        }

        public override void UpdateEntities(ICollection<KeyValue> entities) {
            throw new NotImplementedException();
        }

        public override ICollection<KeyValue> ReadEntities(ICollection<string> ids) {
            var result = new List<KeyValue>();
            foreach (var id in ids) {
                var filePath = FilePath(id);
                string payload = null;
                if (File.Exists(filePath)) {
                    payload = ReadText(filePath);
                    // payload = await File.ReadAllTextAsync(filePath);
                }
                var entry = new KeyValue {
                    key = id,
                    value = new JsonValue{ json = payload }
                };
                result.Add(entry);
            }
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
