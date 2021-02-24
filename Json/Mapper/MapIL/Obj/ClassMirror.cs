// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Utils;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.MapIL.Obj
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
        private     bool                isDerivedType;
        private     TypeMapper          classTypeDbg;  // only for debugging

        public void LoadInstance<T>(TypeCache typeCache, TypeMapper baseType, ref TypeMapper classType, ref T obj) {
            if (baseType.instanceFactory == null) {
                isDerivedType = false;
                classTypeDbg = classType;
                layout = classType.layout;
                primitives.Resize(layout.primCount);
                objects.   Resize(layout.objCount);
                ClassLayout<T> l = (ClassLayout<T>) layout;
                l.LoadObjectToMirror(primitives.array, objects.array, ref obj);
            } else {
                isDerivedType = true;
                classType = typeCache.GetTypeMapper(obj.GetType());
                classTypeDbg = classType;
                layout = classType.layout;
                primitives.Resize(layout.primCount);
                objects.   Resize(layout.objCount);
                layout.LoadObjectToMirror(primitives.array, objects.array, obj);
            }
        }
        
        // ReSharper disable once UnusedMember.Global
        public DbgEntry[] DebugView => GetDebugView();
        
        public void StoreInstance<T>(ref T obj) {
            if (!isDerivedType) {
                ClassLayout<T> l = (ClassLayout<T>) layout;
                l.StoreMirrorToPayload(ref obj, primitives.array, objects.array);
            } else {
                layout.StoreMirrorToPayload(obj, primitives.array, objects.array);
            }
        }
        
        internal void ClearObjectReferences() {
            for (int n = 0; n < objects.Count; n++)
                objects.array[n] = null;
            objects.Resize(0); // prevent clearing already cleared objects
        }
        
        private DbgEntry[] GetDebugView() {
            var fields = classTypeDbg.propFields.fields;
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
        public void StoreDblNull(int idx, double? value) {
            primitives.array[idx] = value.HasValue ? BitConverter.DoubleToInt64Bits((double) value) : default;
        }
        public double? LoadDblNull(int idx) {
            var value = primitives.array[idx];
            return value.HasValue ? (double?)BitConverter.Int64BitsToDouble((long) value) : default;
        }

        public void StoreFltNull(int idx, float? value) {
            primitives.array[idx] = value.HasValue ? BitConverter.SingleToInt32Bits((float)value) : default;
        }
        public float?    LoadFltNull     (int idx) {
            var value = primitives.array[idx];
            // ReSharper disable once PossibleInvalidOperationException
            return value.HasValue ? (float?)BitConverter.Int32BitsToSingle((int)(long) primitives.array[idx]) : default;
        }

        public void StoreLongNull(int idx, long? value) {
            primitives.array[idx] = value;
        }
        
        public long? LoadLongNull(int idx) {
            return primitives.array[idx];
        }

        public void StoreIntNull(int idx, int? value) {
            primitives.array[idx] = value;
        }

        public int? LoadIntNull(int idx) {
            var value = primitives.array[idx];
            return value.HasValue ? (int?)value : default;
        }

        public void StoreShortNull(int idx, short? value) {
            primitives.array[idx] = value;
        }

        public short? LoadShortNull(int idx) {
            var value = primitives.array[idx];
            return value.HasValue ? (short?)value : default;
        }

        public void StoreByteNull(int idx, byte? value) {
            primitives.array[idx] = value;
        }

        public byte? LoadByteNull(int idx) {
            var value = primitives.array[idx];
            return value.HasValue ? (byte?)value : default;
        }

        public void StoreBoolNull(int idx, bool? value) {
            primitives.array[idx] = value.HasValue ? (bool)value ? 1 : 0 : default;
        }

        public bool? LoadBoolNull(int idx) {
            var value = primitives.array[idx];
            return value.HasValue ? (bool?)(primitives.array[idx] != 0) : default;
        }
        //
        public void StorePrimitiveNull(int idx) {
            primitives.array[idx] = null;
        }
        
        public void StoreStructNonNull(int idx) {
            primitives.array[idx] = 1;
        }
        
        public bool LoadPrimitiveHasValue(int idx) {
            var value = primitives.array[idx];
            return value.HasValue;
        }
        
        

        //
        public void     StoreObj    (int idx,            object value) { objects.array[idx] = value; }
        public object   LoadObj     (int idx)  { return                  objects.array[idx]; }
    }
    
    public class DbgEntry {
        public string       index;
        public string       name;
        public object       value;
        public PropField    field;

        public override string ToString() {
            object valueStr = value ?? "null";
            return $"{index}  '{name}':  {valueStr}";
        }
    }
}

#endif

