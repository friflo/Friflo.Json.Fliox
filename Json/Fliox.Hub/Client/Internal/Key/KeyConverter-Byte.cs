// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Json.Fliox.Hub.Client.Internal.Key
{
    internal sealed class KeyConverterByte : KeyConverter<byte>
    {
        internal override byte IdToKey(in JsonKey id) {
            return (byte)id.AsLong();
        }

        internal override JsonKey KeyToId(in byte key) {
            return new JsonKey(key);
        }
    }
    
    internal sealed class KeyConverterByteNull : KeyConverter<byte?>
    {
        internal override   bool                IsKeyNull (byte? key)       => key == null;
        
        internal override byte? IdToKey(in JsonKey id) {
            return (byte)id.AsLong();
        }

        internal override JsonKey KeyToId(in byte? key) {
            return new JsonKey(key);
        }
    }
}