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
    public class ClassPayload : IDisposable
    {

#if !UNITY_5_3_OR_NEWER
        // payload size changes, depending on which class is used at the current classLevel
        private     ValueList<long>     primitives = new ValueList<long>(8, AllocType.Persistent);
        private     ClassLayout         layout;

        public void LoadInstance(TypeMapper classType, object obj) {
            layout = classType.GetClassLayout();
            primitives.Resize(layout.primCount);
            
            layout.loadObjectToPayload(primitives.array, obj);
        }
        
        public void StoreInstance(object obj) {
            layout.storePayloadToObject(obj, primitives.array);
        }
        
        public void Dispose() {
            primitives.Dispose();
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
#else
        // Unity dummies
        public void LoadInstance(TypeMapper classType, object obj) {}
        public void StoreInstance(object obj) {}
        public void Dispose() {}
#endif
    }

    public readonly struct ClassLayout
    {
#if !UNITY_5_3_OR_NEWER
        internal readonly int   primCount;

        internal ClassLayout(Type type, PropertyFields  propFields, ResolverConfig config) {
            primCount       = propFields.primCount;
            
            // create load/store instance expression

            Action<long[], object> load = null;
            Action<object, long[]> store = null;
            if (config.useIL) {
                var loadLambda = ILCodeGen.LoadInstanceExpression(propFields, type);
                load  = loadLambda.Compile();
                
                var storeLambda = ILCodeGen.StoreInstanceExpression(propFields, type);
                store = storeLambda.Compile();
            }
            loadObjectToPayload  = load;
            storePayloadToObject = store;
        }

        internal readonly Action<long[], object>  loadObjectToPayload; 
        internal readonly Action<object, long[]>  storePayloadToObject;
#else
        internal ClassLayout(Type type, PropertyFields  propFields, ResolverConfig config) { }
#endif
    }
}
