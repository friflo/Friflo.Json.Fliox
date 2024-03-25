// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Client.Internal.Key
{
    internal sealed class KeyConverterShort : KeyConverter<short>
    {
        internal override short IdToKey(in JsonKey id) {
            return (short)id.AsLong();
        }

        internal override JsonKey KeyToId(in short key) {
            return new JsonKey(key);
        }
    }
    
    internal sealed class KeyConverterShortNull : KeyConverter<short?>
    {
        internal override   bool                IsKeyNull (short? key)       => key == null;
        
        internal override short? IdToKey(in JsonKey id) {
            return (short)id.AsLong();
        }

        internal override JsonKey KeyToId(in short? key) {
            return new JsonKey(key);
        }
    }
}