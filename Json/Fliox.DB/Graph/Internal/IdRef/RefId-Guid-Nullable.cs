// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph.Internal.IdRef
{
    internal class RefKeyGuidNull<T> : RefKey<Guid?, T> where T : class
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