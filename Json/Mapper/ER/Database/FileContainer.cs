// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Friflo.Json.Mapper.ER.Database
{
    public class FileDatabase : EntityDatabase
    {
        private readonly string databaseFolder;

        public FileDatabase(string databaseFolder) {
            this.databaseFolder = databaseFolder + "/";
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
        }

        private string FilePath(string key) {
            return folder + key + ".json";
        }
        

#pragma warning disable 1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await TaskEx.Run(...)' to do CPU-bound work on a background thread
        public override async Task CreateEntities(ICollection<KeyValue> entities) {
            foreach (var entity in entities) {
                var path = FilePath(entity.key);
                await File.WriteAllTextAsync(path, entity.value);
            }
        }

        public override async Task UpdateEntities(ICollection<KeyValue> entities) {
            throw new NotImplementedException();
        }

        public override async Task<ICollection<KeyValue>> ReadEntities(ICollection<string> ids) {
            var result = new List<KeyValue>();
            foreach (var id in ids) {
                var path = FilePath(id);
                var payload = await File.ReadAllTextAsync(path);
                var entry = new KeyValue {
                    key     = id,
                    value   = payload
                };
                result.Add(entry);
            }
            return result;
        }
#pragma warning restore 1998
    }
}
