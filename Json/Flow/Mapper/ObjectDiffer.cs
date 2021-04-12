// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Mapper.Diff;

namespace Friflo.Json.Flow.Mapper
{
    public class ObjectDiffer : Differ
    {
        public ObjectDiffer(TypeStore typeStore) : base(typeStore) {
        }
    }
}