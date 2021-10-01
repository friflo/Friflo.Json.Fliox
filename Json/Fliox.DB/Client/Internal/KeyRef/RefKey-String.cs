// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Client.Internal.KeyRef
{
    internal sealed class RefKeyString<T> : RefKey<string, T> where T : class {
        internal override   bool                IsKeyNull (string key)      => key == null;

        internal override string IdToKey(in JsonKey id) {
            return id.AsString();
        }

        internal override JsonKey KeyToId(in string key) {
            return new JsonKey(key);
        }
    }
}