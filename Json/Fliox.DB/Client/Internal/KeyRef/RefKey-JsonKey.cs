// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Client.Internal.KeyRef
{
    internal sealed class RefKeyJsonKey<T> : RefKey<JsonKey, T> where T : class {
        internal override   bool                IsKeyNull (JsonKey key)      => key.IsNull();

        internal override JsonKey IdToKey(in JsonKey id) {
            return id;
        }

        internal override JsonKey KeyToId(in JsonKey key) {
            return key;
        }
    }
}