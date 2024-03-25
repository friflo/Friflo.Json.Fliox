// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Client.Internal.Key
{
    internal sealed class KeyConverterShortString : KeyConverter<ShortString> {
        internal override   bool                IsKeyNull (ShortString key)      => key.IsNull();

        internal override ShortString   IdToKey(in JsonKey id) {
            return new ShortString(id);
        }

        internal override JsonKey       KeyToId(in ShortString key) {
            return new JsonKey(key);
        }
    }
}