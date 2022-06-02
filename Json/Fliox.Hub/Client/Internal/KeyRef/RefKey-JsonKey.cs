// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyRef
{
    internal sealed class RefKeyJsonKey : RefKey<JsonKey> {
        internal override   bool                IsKeyNull (JsonKey key)      => key.IsNull();

        internal override JsonKey IdToKey(in JsonKey id) {
            return id;
        }

        internal override JsonKey KeyToId(in JsonKey key) {
            return key;
        }
    }
}