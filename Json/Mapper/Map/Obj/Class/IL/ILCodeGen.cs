// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;
using Exp = System.Linq.Expressions.Expression;
    
namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{
    public static class ILCodeGen
    {
        // Nice Blog about expression trees:
        // [Working with Expression Trees in C# | Alexey Golub] https://tyrrrz.me/blog/expression-trees
        //
        // The idea of loading/storing all fields of a class with one method call came to this nice blog:
        // [Optimizing reflection in C# via dynamic code generation | by Sergio Pedri | Medium]
        // https://medium.com/@SergioPedri/optimizing-reflection-with-dynamic-code-generation-6e15cef4b1a2
        internal static Expression<Action<long[], object[], object>> LoadInstanceExpression (PropertyFields propFields, Type type) {
            var dst         = Exp.Parameter(typeof(long[]),   "dst");     // parameter: long[]   dst;
            var dstObj      = Exp.Parameter(typeof(object[]), "dstObj");  // parameter: object[] dstObj;
            var src         = Exp.Parameter(typeof(object),   "src");     // parameter: object   src;
            
            var srcTyped    = Exp.Convert(src, type);                   // <Type> srcTyped = (<Type>)src;
            
            var doubleToInt64Bits = typeof(BitConverter).GetMethod(nameof(BitConverter.DoubleToInt64Bits));
            var singleToInt32Bits = typeof(BitConverter).GetMethod(nameof(BitConverter.SingleToInt32Bits));

            var assignmentList = new List<BinaryExpression>();
            for (int n = 0; n < propFields.fields.Length; n++) {
                PropField field = propFields.fields[n];
                Type fieldType  = field.fieldTypeNative; 
                if (!field.isValueType || !field.fieldTypeNative.IsPrimitive)
                    continue;
                
                MemberExpression memberVal;
                if (field.field != null)
                    memberVal   = Exp.Field(   srcTyped, field.name);           // memberVal = srcTyped.<field.name>;
                else
                    memberVal   = Exp.Property(srcTyped, field.name);           // memberVal = srcTyped.<field.name>;
                
                Expression longVal;
                if (fieldType == typeof(long) || fieldType == typeof(int) ||fieldType == typeof(short) || fieldType == typeof(byte)) {
                    longVal     = Exp.Convert(memberVal, typeof(long));         // longVal   = (long)memberVal;
                } else if (fieldType == typeof(bool)) {
                    longVal     = Exp.Condition(memberVal, Exp.Constant(1L), Exp.Constant(0L)); // longVal   = memberVal ? 1 : 0;
                } else if (fieldType == typeof(double)) {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    longVal     = Exp.Call(doubleToInt64Bits, memberVal);       // longVal = BitConverter.DoubleToInt64Bits(memberVal);
                } else if (fieldType == typeof(float)) {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var intVal  = Exp.Call(singleToInt32Bits, memberVal);       // intVal  = BitConverter.SingleToInt32Bits(memberVal);
                    longVal     = Exp.Convert(intVal, typeof(long));            // longVal = (long)intVal;
                }
                else
                    throw new InvalidOperationException("Unexpected primitive type: " + fieldType);

                var arrayIndex  = Exp.Constant(n, typeof(int));                 // int arrayIndex = <field index>;
                var dstElement  = Exp.ArrayAccess(dst, arrayIndex);             // ref long[] dstElement = ref dst[arrayIndex];

                var dstAssign   = Exp.Assign(dstElement, longVal);              // dstElement = longVal;
                assignmentList.Add(dstAssign);
            }
            var assignmentsBlock= Exp.Block(assignmentList);
            
            var lambda = Exp.Lambda<Action<long[], object[], object>> (assignmentsBlock, dst, dstObj, src);
            return lambda;
        }

        internal static Expression<Action<object, long[], object[]>> StoreInstanceExpression (PropertyFields propFields, Type type) {
            var dst         = Exp.Parameter(typeof(object),   "dst");       // parameter: long[]   dst;
            var src         = Exp.Parameter(typeof(long[]),   "src");       // parameter: object   src;
            var srcObj      = Exp.Parameter(typeof(object[]), "srcObj");    // parameter: object[] srcObj;
            
            var dstTyped    = Exp.Convert(dst, type);                       // <Type> dstTyped = (<Type>)dst;
            
            var int64BitsToDouble = typeof(BitConverter).GetMethod(nameof(BitConverter.Int64BitsToDouble));
            var int32BitsToSingle = typeof(BitConverter).GetMethod(nameof(BitConverter.Int32BitsToSingle));

            var assignmentList = new List<BinaryExpression>();
            for (int n = 0; n < propFields.fields.Length; n++) {
                PropField field = propFields.fields[n];
                Type fieldType  = field.fieldTypeNative; 
                if (!field.isValueType || !field.fieldTypeNative.IsPrimitive)
                    continue;
                
                var arrayIndex  = Exp.Constant(n, typeof(int));                 // int arrayIndex = <field index>;
                var srcElement  = Exp.ArrayAccess(src, arrayIndex);             // ref long[] srcElement = ref src[arrayIndex];
                Expression srcTyped;
                if (fieldType == typeof(long) || fieldType == typeof(int) ||fieldType == typeof(short) || fieldType == typeof(byte)) {
                    srcTyped    = Exp.Convert(srcElement, fieldType);           // srcTyped  = (<Field Type>)srcElement;
                } else if (fieldType == typeof(bool)) {
                    var not0    = Exp.NotEqual(srcElement, Exp.Constant(0L));
                    srcTyped    = Exp.Condition(not0, Exp.Constant(true), Exp.Constant(false)); // srcTyped = srcElement != 0;
                } else if (fieldType == typeof(double)) {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    srcTyped    = Exp.Call(int64BitsToDouble, srcElement);      // srcTyped = BitConverter.Int64BitsToDouble (srcElement);
                } else if (fieldType == typeof(float)) {
                    var srcInt  = Exp.Convert(srcElement, typeof(int));         // srcInt   = (int)srcElement;
                    // ReSharper disable once AssignNullToNotNullAttribute
                    srcTyped    = Exp.Call(int32BitsToSingle, srcInt);          // srcTyped = BitConverter.Int32BitsToSingle (srcInt);
                }
                else
                    throw new InvalidOperationException("Unexpected primitive type: " + fieldType);

                MemberExpression dstMember;
                if (field.field != null)
                    dstMember   = Exp.Field   (dstTyped, field.name);           // ref dstMember = ref dstTyped.<field.name>;
                else
                    dstMember   = Exp.Property(dstTyped, field.name);           // ref dstMember = ref dstTyped.<field.name>;

                var dstAssign   = Exp.Assign(dstMember, srcTyped);              // dstMember = srcTyped;
                assignmentList.Add(dstAssign);
            }
            var assignmentsBlock= Exp.Block(assignmentList);
            
            var lambda = Exp.Lambda<Action<object, long[], object[]>> (assignmentsBlock, dst, src, srcObj);
            return lambda;
        }
    }
}

#endif
