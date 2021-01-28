// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;

namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{
    /// <summary>
    /// This class has two main purposes: 
    /// 1. Load the fields of a class instance into the <see cref="data"/> array.
    /// 2. Store the "instances fields" represented by the <see cref="data"/> array to the fields of a given class instance.
    ///  
    /// This class contains IL specific state/data which is used by JsonReader & JsonWriter. So its not thread safe. 
    /// </summary>
    public class ClassPayload : IDisposable
    {
        // payload size changes, depending on which class is used at the current classLevel
        private     ValueList<long>     data = new ValueList<long>(8, AllocType.Persistent);
        private     ClassLayout         layout;

        public void LoadInstance(TypeMapper classType, object obj) {
            layout = classType.GetClassLayout();
            data.Resize(layout.size);
            
            layout.loadObjectToPayload(data.array, obj);
        }
        
        public void StoreInstance(object obj) {
            // call store instance expression delegate
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
        internal readonly int   size;
        internal readonly int[] fieldPos;


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
            
            // create load/store instance expression

            var loadLambda = LoadInstanceExpression(propFields, type);
            loadObjectToPayload = loadLambda.Compile();
            
            // var storeLambda = StoreInstanceExpression(propFields, type);
            // storePayloadToObject = storeLambda.Compile();
        }

        internal readonly Action<long[], object>  loadObjectToPayload; 
    //  internal readonly Action<object, long[]>  storePayloadToObject;


        // Nice Blog about expression trees:
        // [Working with Expression Trees in C# | Alexey Golub] https://tyrrrz.me/blog/expression-trees
        //
        private static Expression<Action<long[], object>> LoadInstanceExpression (PropertyFields propFields, Type type) {
            var dst         = Expression.Parameter(typeof(long[]), "dst");      // parameter: long[] dst
            var src         = Expression.Parameter(typeof(object), "src");      // parameter: object src;
            
            var srcTyped    = Expression.Convert(src, type);                    // <Type> srcTyped = (<Type>)src;

            var assignmentList = new List<BinaryExpression>();
            for (int n = 0; n < propFields.fields.Length; n++) {
                PropField field = propFields.fields[n];
                if (!field.isValueType || !field.fieldTypeNative.IsPrimitive)
                    continue;
                
                var memberVal   = Expression.PropertyOrField(srcTyped, field.name); // memberVal = srcTyped.<field.name>;
                var longVal     = Expression.Convert(memberVal, typeof(long));      // longVal   = (long)memberVal; 
                
                var arrayIndex  = Expression.Constant(n, typeof(int));              // int arrayIndex = <field index>;
                var dstElement  = Expression.ArrayAccess(dst, arrayIndex);          // ref long[] dstElement = ref dst[arrayIndex];

                var dstAssign   = Expression.Assign(dstElement, longVal);           // dstElement = longVal;
                assignmentList.Add(dstAssign);
            }
            var assignmentsBlock = Expression.Block(assignmentList);
            
            var lambda = Expression.Lambda<Action<long[], object>> (assignmentsBlock, dst, src);
            return lambda;
        }
        

        private static Expression<Action<object, long[]>> StoreInstanceExpression (PropertyFields propFields, Type type) {
            var dst         = Expression.Parameter(typeof(object), "dst");      // parameter: long[] dst
            var src         = Expression.Parameter(typeof(long[]), "src");      // parameter: object src;
            
            var dstTyped    = Expression.Convert(dst, type);                    // <Type> dstTyped = (<Type>)dst;

            var assignmentList = new List<BinaryExpression>();
            for (int n = 0; n < propFields.fields.Length; n++) {
                PropField field = propFields.fields[n];
                if (!field.isValueType || !field.fieldTypeNative.IsPrimitive)
                    continue;
                
                var arrayIndex  = Expression.Constant(n, typeof(int));                  // int arrayIndex = <field index>;
                var srcElement  = Expression.ArrayAccess(src, arrayIndex);              // ref long[] srcElement = ref src[arrayIndex];
                
                var srcTyped    = Expression.Convert(srcElement, field.fieldTypeNative);// srcTyped  = (<Field Type>)srcElement; 
                var dstMember   = Expression.PropertyOrField(dstTyped, field.name);     // ref dstMember = ref dstTyped.<field.name>;

                var dstAssign   = Expression.Assign(dstMember, srcTyped);               // dstMember = srcTyped;
                assignmentList.Add(dstAssign);
            }
            var assignmentsBlock = Expression.Block(assignmentList);
            
            var lambda = Expression.Lambda<Action<object, long[]>> (assignmentsBlock, dst, src);
            return lambda;
        }

    }

}