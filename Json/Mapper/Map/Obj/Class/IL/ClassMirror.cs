// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{

    /// <summary>
    /// This class has two main purposes: 
    /// 1. Load the fields of a class instance into the <see cref="primitives"/> array.
    /// 2. Store the "instances fields" represented by the <see cref="primitives"/> array to the fields of a given class instance.
    ///  
    /// This class contains IL specific state/data which is used by JsonReader & JsonWriter. So its not thread safe. 
    /// </summary>
    public class ClassMirror : IDisposable
    {

        // payload size changes, depending on which class is used at the current classLevel
        private     ValueList<long?>    primitives = new ValueList<long?>  (8, AllocType.Persistent);
        private     ValueList<object>   objects    = new ValueList<object>(8, AllocType.Persistent);
        private     ClassLayout         layout;
        private     TypeMapper          classTypeDbg;  // only for debugging

        public void LoadInstance<T>(TypeMapper classType, T obj) {
            classTypeDbg = classType;
            layout = classType.GetClassLayout();
            primitives.Resize(layout.primCount);
            objects.   Resize(layout.objCount);
            ClassLayout<T> l = (ClassLayout<T>) layout;
            l.LoadObjectToMirror(primitives.array, objects.array, obj);
        }
        
        public void StoreInstance<T>(T obj) {
            ClassLayout<T> l = (ClassLayout<T>) layout;
            l.StoreMirrorToPayload(obj, primitives.array, objects.array);
        }
        
        internal void ClearObjectReferences() {
            for (int n = 0; n < objects.Count; n++)
                objects.array[n] = null;
            objects.Resize(0); // prevent clearing already cleared objects
        }

        public class DbgEntry {
            public string       index;
            public string       name;
            public object       value;
            public PropField    field;

            public override string ToString() {
                // ReSharper disable once MergeConditionalExpression
                object valueStr = value == null ? "null" : value;
                return $"{index}  '{name}':  {valueStr}";
            }
        }

        // ReSharper disable once UnusedMember.Global
        public DbgEntry[] GetDebugView() {
            var fields = classTypeDbg.GetPropFields().fields;
            DbgEntry[] entries = new DbgEntry[fields.Length];
            for (int n = 0; n < fields.Length; n++) {
                var field = fields[n];
                var isValueType = field.fieldType.isValueType;
                var entry = new DbgEntry {
                    name  = field.name,
                    index = isValueType ? "prim: " + field.primIndex : "obj: " + field.objIndex,
                    value = isValueType ? primitives.array[field.primIndex] : objects.array[field.objIndex],
                    field = field
                };
                entries[n] = entry;
            }
            return entries;
        }

        public void Dispose() {
            primitives.Dispose();
            objects.   Dispose();
        }
        // ----------------------------------
        public void     StoreDbl    (int idx,           double value) {  primitives.array[idx] = BitConverter.DoubleToInt64Bits(value); }
        public double   LoadDbl     (int idx) {
            return BitConverter.Int64BitsToDouble(                 (long)primitives.array[idx]); }
        
        public void     StoreFlt    (int idx,           float value) {   primitives.array[idx] = BitConverter.SingleToInt32Bits(value); }
        public float    LoadFlt     (int idx) {
            return BitConverter.Int32BitsToSingle(                  (int)primitives.array[idx]); }

        public void     StoreLong   (int idx,            long value)   { primitives.array[idx] = value; }
        public long     LoadLong    (int idx)             { return (long)primitives.array[idx]; }
        
        public void     StoreInt    (int idx,            int value)    { primitives.array[idx] = value; }
        public int      LoadInt     (int idx)  { return (int)            primitives.array[idx]; }
        
        public void     StoreShort  (int idx,            short value)  { primitives.array[idx] = value; }
        public short    LoadShort   (int idx)  { return (short)          primitives.array[idx]; }
        
        public void     StoreByte   (int idx,            byte value)   { primitives.array[idx] = value; }
        public byte     LoadByte    (int idx)  { return (byte)           primitives.array[idx]; }
        
        public void     StoreBool   (int idx,            bool value)   { primitives.array[idx] = value ? 1 : 0; }
        public bool     LoadBool    (int idx)  { return                  primitives.array[idx] != 0; }


        
        // ----------------------------------
        public void StoreDblNulL(int idx, double? value) {
            primitives.array[idx] = value.HasValue ? BitConverter.DoubleToInt64Bits((double) value) : default;
        }
        public double? LoadDblNulL(int idx) {
            var value = primitives.array[idx];
            return value.HasValue ? (double?)BitConverter.Int64BitsToDouble((long) value) : default;
        }

        public void StoreFltNulL(int idx, float? value) {
            primitives.array[idx] = value.HasValue ? BitConverter.SingleToInt32Bits((float)value) : default;
        }
        public float?    LoadFltNulL     (int idx) {
            var value = primitives.array[idx];
            // ReSharper disable once PossibleInvalidOperationException
            return value.HasValue ? (float?)BitConverter.Int32BitsToSingle((int)(long) primitives.array[idx]) : default;
        }

        public void StoreLongNulL(int idx, long? value) {
            primitives.array[idx] = value;
        }
        
        public long? LoadLongNulL(int idx) {
            return primitives.array[idx];
        }

        public void StoreIntNulL(int idx, int? value) {
            primitives.array[idx] = value;
        }

        public int? LoadIntNulL(int idx) {
            var value = primitives.array[idx];
            return value.HasValue ? (int?)value : default;
        }

        public void StoreShortNulL(int idx, short? value) {
            primitives.array[idx] = value;
        }

        public short? LoadShortNulL(int idx) {
            var value = primitives.array[idx];
            return value.HasValue ? (short?)value : default;
        }

        public void StoreByteNulL(int idx, byte? value) {
            primitives.array[idx] = value;
        }

        public byte? LoadByteNulL(int idx) {
            var value = primitives.array[idx];
            return value.HasValue ? (byte?)value : default;
        }

        public void StoreBoolNulL(int idx, bool? value) {
            primitives.array[idx] = value.HasValue ? (bool)value ? 1 : 0 : default;
        }

        public bool? LoadBoolNulL(int idx) {
            var value = primitives.array[idx];
            return value.HasValue ? (bool?)(primitives.array[idx] != 0) : default;
        }
        //
        public void StorePrimitiveNull(int idx) {
            primitives.array[idx] = null;
        }
        
        public bool LoadPrimitiveHasValue(int idx) {
            var value = primitives.array[idx];
            return value.HasValue;
        }
        
        

        //
        public void     StoreObj    (int idx,            object value) { objects.array[idx] = value; }
        public object   LoadObj     (int idx)  { return                  objects.array[idx]; }
    }
}

#endif

