// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Client.Internal.Key
{
    internal sealed class KeyConverterInt : KeyConverter<int>
    {
        internal override int IdToKey(in JsonKey id) {
            return (int)id.AsLong();
        }

        internal override JsonKey KeyToId(in int key) {
            return new JsonKey(key);
        }
    }
    
    internal sealed class KeyConverterIntNull : KeyConverter<int?>
    {
        internal override   bool                IsKeyNull (int? key)       => key == null;
        
        internal override int? IdToKey(in JsonKey id) {
            return (int)id.AsLong();
        }

        internal override JsonKey KeyToId(in int? key) {
            return new JsonKey(key);
        }
    }
}