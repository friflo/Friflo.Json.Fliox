// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Hub.Client.Internal.Key
{
    internal sealed class KeyConverterGuid : KeyConverter<Guid>
    {
        internal override Guid IdToKey(in JsonKey id) {
            return id.AsGuid();
        }

        internal override JsonKey KeyToId(in Guid key) {
            return new JsonKey(key);
        }
    }
    
    internal sealed class KeyConverterGuidNull : KeyConverter<Guid?>
    {
        internal override   bool                IsKeyNull (Guid? key)       => key == null;

        internal override Guid? IdToKey(in JsonKey id) {
            return id.AsGuidNullable();
        }

        internal override JsonKey KeyToId(in Guid? key) {
            return new JsonKey(key);
        }
    }
}