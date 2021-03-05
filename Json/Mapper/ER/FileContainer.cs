// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Friflo.Json.Mapper.ER
{
    public class FileContainer<T> : EntityContainer<T> where T : Entity
    {
        private readonly string folder;
        
        public FileContainer(EntityDatabase database, string folder) : base (database) {
            this.folder = folder;
        }
        
        public override int Count => throw new NotImplementedException();

#pragma warning disable 1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await TaskEx.Run(...)' to do CPU-bound work on a background thread
        public override async Task AddEntities(IEnumerable<T> entities) {
            throw new NotImplementedException();
        }
        
        public override async Task UpdateEntities(IEnumerable<T> entities) {
            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<T>> GetEntities(IEnumerable<string> ids) {
            throw new NotImplementedException();
            // var result = new List<T>();
            // return result;
        }
#pragma warning restore 1998
    }
}