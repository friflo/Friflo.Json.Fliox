// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.Class.IL
{
    // This class contains IL specific state/data which is used by JsonReader & JsonWriter. So its not thread safe.
    public class ClassPayload : IDisposable
    {
        // payload size changes, depending on which class is used at the current classLevel
        private     ValueList<long>     data = new ValueList<long>(8, AllocType.Persistent);
        private     ClassLayout         layout;

        public void InitClassPayload(TypeMapper classType) {
            layout = classType.GetClassLayout();
            data.Resize(layout.size);
        }
        
        public void Dispose() {
            data.Dispose();
        }
        
#if !UNITY_5_3_OR_NEWER
        public void     StoreDbl    (int idx,           double value) {  data.array[layout.fieldPos[idx]] = BitConverter.DoubleToInt64Bits(value); }
        public double   LoadDbl     (int idx) {
            return BitConverter.Int64BitsToDouble(                       data.array[layout.fieldPos[idx]]); }
        
        public void     StoreFlt    (int idx,           float value) {   data.array[layout.fieldPos[idx]] = BitConverter.SingleToInt32Bits(value); }
        public float    LoadFlt     (int idx) {
            return BitConverter.Int32BitsToSingle(                  (int)data.array[layout.fieldPos[idx]]); }

        public void     StoreLong   (int idx,            long value)   { data.array[layout.fieldPos[idx]] = value; }
        public long     LoadLong    (int idx)                   { return data.array[layout.fieldPos[idx]]; }
        
        public void     StoreInt    (int idx,            int value)    { data.array[layout.fieldPos[idx]] = value; }
        public int      LoadInt     (int idx)  { return (int)            data.array[layout.fieldPos[idx]]; }
        
        public void     StoreShort  (int idx,            short value)  { data.array[layout.fieldPos[idx]] = value; }
        public short    LoadShort   (int idx)  { return (short)          data.array[layout.fieldPos[idx]]; }
        
        public void     StoreByte   (int idx,            byte value)   { data.array[layout.fieldPos[idx]] = value; }
        public byte     LoadByte    (int idx)  { return (byte)           data.array[layout.fieldPos[idx]]; }
        
        public void     StoreBool   (int idx,            bool value)   { data.array[layout.fieldPos[idx]] = value ? 1 : 0; }
        public bool     LoadBool    (int idx)  { return                  data.array[layout.fieldPos[idx]] != 0; }
#endif

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
                tempPos[n] = n; // fake pos;
                count ++;
            }
            size       = count;
            fieldPos   = tempPos;
        }
    }

}