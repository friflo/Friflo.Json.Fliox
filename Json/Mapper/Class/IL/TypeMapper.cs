// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using Friflo.Json.Mapper.Class;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Mapper.Map
{
    // This class is shared via multiple JsonReader / JsonWriter instances which run in various threads.
    // So its must not contain any mutable state.
    public partial class TypeMapper
    {
        protected void InitClassLayout(PropertyFields fields) {
            var x = type;
        }
    }
}