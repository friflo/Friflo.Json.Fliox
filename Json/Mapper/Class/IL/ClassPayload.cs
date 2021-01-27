// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.Class.IL
{
    // This class contains IL specific state/data which is used by JsonReader & JsonWriter. So its not thread safe.
    public class ClassPayload : IDisposable
    {
        // payload size changes, depending on which class is used at the current classLevel
        private     ValueList<byte>     data = new ValueList<byte>(32, AllocType.Persistent);
        private     ClassLayout         layout;

        public void InitClassPayload(TypeMapper classType) {
            layout = classType.GetClassLayout();
            data.Resize(layout.size);
        }
        
        public void Dispose() {
            data.Dispose();
        }

        public void StoreInt(int fieldPos, int value) {
            int start = layout.fieldPos[fieldPos];
            MemoryMarshal.Write(new Span<byte>(data.array, start, 4), ref value);
        }

        public int LoadInt(int fieldPos) {
            int start = layout.fieldPos[fieldPos];
            return MemoryMarshal.Read<int>(new Span<byte>(data.array, start, 4));
        }
    }

    public readonly struct ClassLayout
    {
        internal readonly int       size;
        internal readonly int[]     fieldPos;

        internal ClassLayout(Type type, PropertyFields  propFields) {
            var fields = propFields.fields;
            int count = 0;
            int[] tempPos = new int[fields.Length]; 
            for (int n = 0; n < fields.Length; n++) {
                tempPos[n] = 4 * n; // fake pos;
                count += 4;
            }
            size       = count;
            fieldPos   = tempPos;
        }
    }

}