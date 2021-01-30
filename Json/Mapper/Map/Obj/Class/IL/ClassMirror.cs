// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;

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

#if !UNITY_5_3_OR_NEWER
        // payload size changes, depending on which class is used at the current classLevel
        private     ValueList<long>     primitives = new ValueList<long>  (8, AllocType.Persistent);
        private     ValueList<object>   objects    = new ValueList<object>(8, AllocType.Persistent);
        private     ClassLayout         layout;

        public void LoadInstance(TypeMapper classType, object obj) {
            layout = classType.GetClassLayout();
            primitives.Resize(layout.primCount);
            objects.   Resize(layout.objCount);
            
            layout.LoadObjectToMirror(primitives.array, objects.array, obj);
        }
        
        public void StoreInstance(object obj) {
            layout.StoreMirrorToPayload(obj, primitives.array, objects.array);
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

#else
        // Unity dummies
        public      void    LoadInstance    (TypeMapper classType, object obj) {}
        public      void    StoreInstance   (object obj) {}
        internal    void    ClearObjectReferences() {}
        public      void    Dispose         () {}
        public      void    StoreObj        (int idx,object value) {  }
        public      object  LoadObj         (int idx)  { return null; }
#endif
    }
    
    public abstract class ClassLayout {

        internal readonly int   primCount;
        internal readonly int   objCount;
        
        internal ClassLayout(PropertyFields  propFields) {
            primCount       = propFields.primCount;
            objCount        = propFields.objCount;
        }

        public abstract void LoadObjectToMirror  (long[] dstPrim, object[] dstObj, object src);
        public abstract void StoreMirrorToPayload(object dst, long[] srcPrim, object[] srcObj);
    }
    
    
#if !UNITY_5_3_OR_NEWER
    public class ClassLayout<T> : ClassLayout
    {
        internal ClassLayout(PropertyFields propFields) : base(propFields) {
            loadObjectToPayload = null;
            storePayloadToObject = null;
        }

        internal void InitClassLayout(Type type, PropertyFields propFields, ResolverConfig config) {

            // create load/store instance expression
            Action<long[], object[], T> load = null;
            Action<T, long[], object[]> store = null;
            if (config.useIL) {
                var loadLambda = ILCodeGen.LoadInstanceExpression<T>(propFields);
                load = loadLambda.Compile();

                var storeLambda = ILCodeGen.StoreInstanceExpression<T>(propFields);
                store = storeLambda.Compile();
            }

            loadObjectToPayload = load;
            storePayloadToObject = store;
        }

        public override void LoadObjectToMirror(long[] dstPrim, object[] dstObj, object src) {
            loadObjectToPayload(dstPrim, dstObj, (T) src);
        }

        public override void StoreMirrorToPayload(object dst, long[] srcPrim, object[] srcObj) {
            storePayloadToObject((T) dst, srcPrim, srcObj);
        }

        internal Action<long[], object[], T> loadObjectToPayload;
        internal Action<T, long[], object[]> storePayloadToObject;

    
        // Unity dummies
        // internal        ClassLayout(PropertyFields propFields) : base(propFields) { }
        // internal void   InitClassLayout (Type type, PropertyFields propFields, ResolverConfig config) { }
    }
#endif
}
