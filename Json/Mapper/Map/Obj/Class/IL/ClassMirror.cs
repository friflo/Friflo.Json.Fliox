// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;

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
        private     ValueList<long>     primitives = new ValueList<long>  (8, AllocType.Persistent);
        private     ValueList<object>   objects    = new ValueList<object>(8, AllocType.Persistent);
        private     ClassLayout         layout;

        public void LoadInstance<T>(TypeMapper classType, T obj) {
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
        
        public void Dispose() {
            primitives.Dispose();
            objects.   Dispose();
        }
        
        public void     StoreDbl    (int idx,           double value) {  primitives.array[idx] = BitConverter.DoubleToInt64Bits(value); }
        public double   LoadDbl     (int idx) {
            return BitConverter.Int64BitsToDouble(                       primitives.array[idx]); }
        
        public void     StoreFlt    (int idx,           float value) {   primitives.array[idx] = BitConverter.SingleToInt32Bits(value); }
        public float    LoadFlt     (int idx) {
            return BitConverter.Int32BitsToSingle(                  (int)primitives.array[idx]); }

        public void     StoreLong   (int idx,            long value)   { primitives.array[idx] = value; }
        public long     LoadLong    (int idx)                   { return primitives.array[idx]; }
        
        public void     StoreInt    (int idx,            int value)    { primitives.array[idx] = value; }
        public int      LoadInt     (int idx)  { return (int)            primitives.array[idx]; }
        
        public void     StoreShort  (int idx,            short value)  { primitives.array[idx] = value; }
        public short    LoadShort   (int idx)  { return (short)          primitives.array[idx]; }
        
        public void     StoreByte   (int idx,            byte value)   { primitives.array[idx] = value; }
        public byte     LoadByte    (int idx)  { return (byte)           primitives.array[idx]; }
        
        public void     StoreBool   (int idx,            bool value)   { primitives.array[idx] = value ? 1 : 0; }
        public bool     LoadBool    (int idx)  { return                  primitives.array[idx] != 0; }

        public void     StoreObj    (int idx,            object value) { objects.array[idx] = value; }
        public object   LoadObj     (int idx)  { return                  objects.array[idx]; }
    }
}

#endif

