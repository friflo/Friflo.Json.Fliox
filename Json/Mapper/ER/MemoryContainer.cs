// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Friflo.Json.Mapper.ER
{
    public class MemoryContainer<T> : EntityContainer<T> where T : Entity
    {
        private readonly Dictionary<string, T>  map                 = new Dictionary<string, T>();

        public override int Count => map.Count;

#pragma warning disable 1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await TaskEx.Run(...)' to do CPU-bound work on a background thread
        public override async Task AddEntities(IEnumerable<T> entities) {
            foreach (var entity in entities) {
                map.Add(entity.id, entity);
            }
        }

        public override async Task<IEnumerable<T>> GetEntities(IEnumerable<string> ids) {
            var result = new List<T>();
            foreach (var id in ids) {
                result.Add(map[id]);
            }
            return result;
        }
#pragma warning restore 1998
    }
}