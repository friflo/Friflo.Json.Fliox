// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using Friflo.Json.Mapper.Class;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Mapper.Map
{
    // This class is shared via multiple JsonReader / JsonWriter instances which run in various threads.
    // So its must not contain any mutable state.
    public partial class TypeMapper
    {
        internal  readonly  ClassLayout     layout;
        protected readonly  PropertyFields  propFields;
        
        static ClassLayout CreateClassLayout(Type type, PropertyFields  propFields) {
            if (propFields == null)
                return new ClassLayout();

            var fields = propFields.fields;
            int size = 0;
            int[] fieldPos = new int[fields.Length]; 
            for (int n = 0; n < fields.Length; n++) {
                fieldPos[n] = 4 * n; // fake pos;
                size += 4;
            }
            return new ClassLayout( size, fieldPos);
        }
    }

    struct ClassLayout
    {
        internal readonly int       size;
        internal readonly int[]     fieldPos;

        internal ClassLayout(int size, int[] fieldPos) {
            this.size       = size;
            this.fieldPos   = fieldPos;
        }
    }
}