// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph.Internal.IdRef
{
    internal class RefKeyInt<T> : RefKey<int, T> where T : class
    {
        internal override int IdToKey(in JsonKey id) {
            return (int)id.AsLong();
        }

        internal override JsonKey KeyToId(in int key) {
            return new JsonKey(key);
        }
    }
}