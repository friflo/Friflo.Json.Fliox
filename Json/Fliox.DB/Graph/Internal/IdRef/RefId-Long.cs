// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph.Internal.IdRef
{
    internal class RefKeyLong<T> : RefKey<long, T> where T : class
    {
        internal override   Type                GetKeyType() => typeof(long);

        internal override long IdToKey(in JsonKey id) {
            return id.AsLong();
        }

        internal override JsonKey KeyToId(in long key) {
            return new JsonKey(key);
        }
    }
}