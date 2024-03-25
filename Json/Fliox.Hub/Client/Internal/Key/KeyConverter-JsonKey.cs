// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Client.Internal.Key
{
    internal sealed class KeyConverterJsonKey : KeyConverter<JsonKey> {
        internal override   bool                IsKeyNull (JsonKey key)      => key.IsNull();

        internal override JsonKey IdToKey(in JsonKey id) {
            return id;
        }

        internal override JsonKey KeyToId(in JsonKey key) {
            return key;
        }
    }
}