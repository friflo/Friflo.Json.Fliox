// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph.Internal.IdRef
{
    internal class RefKeyShort<T> : RefKey<short, T> where T : class
    {
        internal override   Type                GetKeyType() => typeof(short);

        internal override short IdToKey(in JsonKey id) {
            return (short)id.AsLong();
        }

        internal override JsonKey KeyToId(in short key) {
            return new JsonKey(key);
        }
    }
}