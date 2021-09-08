// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph.Internal.IdRef
{
    internal class RefKeyByte<T> : RefKey<byte, T> where T : class
    {
        internal override byte IdToKey(in JsonKey id) {
            return (byte)id.AsLong();
        }

        internal override JsonKey KeyToId(in byte key) {
            return new JsonKey(key);
        }
    }
}