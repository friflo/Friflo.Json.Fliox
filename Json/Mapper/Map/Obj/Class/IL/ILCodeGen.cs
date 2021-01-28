// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;

namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{
    public static class ILCodeGen
    {
        // Nice Blog about expression trees:
        // [Working with Expression Trees in C# | Alexey Golub] https://tyrrrz.me/blog/expression-trees
        //
        internal static Expression<Action<long[], object>> LoadInstanceExpression (PropertyFields propFields, Type type) {
            var dst         = Expression.Parameter(typeof(long[]), "dst");      // parameter: long[] dst
            var src         = Expression.Parameter(typeof(object), "src");      // parameter: object src;
            
            var srcTyped    = Expression.Convert(src, type);                    // <Type> srcTyped = (<Type>)src;

            var assignmentList = new List<BinaryExpression>();
            for (int n = 0; n < propFields.fields.Length; n++) {
                PropField field = propFields.fields[n];
                Type fieldType  = field.fieldTypeNative; 
                if (!field.isValueType || !field.fieldTypeNative.IsPrimitive)
                    continue;
                
                var memberVal   = Expression.PropertyOrField(srcTyped, field.name); // memberVal = srcTyped.<field.name>;
                
                Expression longVal = null;
                if (fieldType == typeof(long) || fieldType == typeof(int) ||fieldType == typeof(short) || fieldType == typeof(byte)) {
                    longVal     = Expression.Convert(memberVal, typeof(long));      // longVal   = (long)memberVal;
                } else if (fieldType == typeof(bool)) {
                    longVal     = Expression.Condition(memberVal, Expression.Constant(1L), Expression.Constant(0L)); // longVal   = memberVal ? 1 : 0;
                } else if (fieldType == typeof(double)) {
                    
                } else if (fieldType == typeof(float)) {
                    
                }
                else
                    throw new InvalidOperationException("Unexpected primitive type: " + fieldType);

                var arrayIndex  = Expression.Constant(n, typeof(int));              // int arrayIndex = <field index>;
                var dstElement  = Expression.ArrayAccess(dst, arrayIndex);          // ref long[] dstElement = ref dst[arrayIndex];

                var dstAssign   = Expression.Assign(dstElement, longVal);           // dstElement = longVal;
                assignmentList.Add(dstAssign);
            }
            var assignmentsBlock = Expression.Block(assignmentList);
            
            var lambda = Expression.Lambda<Action<long[], object>> (assignmentsBlock, dst, src);
            return lambda;
        }

        internal static Expression<Action<object, long[]>> StoreInstanceExpression (PropertyFields propFields, Type type) {
            var dst         = Expression.Parameter(typeof(object), "dst");      // parameter: long[] dst
            var src         = Expression.Parameter(typeof(long[]), "src");      // parameter: object src;
            
            var dstTyped    = Expression.Convert(dst, type);                    // <Type> dstTyped = (<Type>)dst;

            var assignmentList = new List<BinaryExpression>();
            for (int n = 0; n < propFields.fields.Length; n++) {
                PropField field = propFields.fields[n];
                Type fieldType  = field.fieldTypeNative; 
                if (!field.isValueType || !field.fieldTypeNative.IsPrimitive)
                    continue;
                
                var arrayIndex  = Expression.Constant(n, typeof(int));                  // int arrayIndex = <field index>;
                var srcElement  = Expression.ArrayAccess(src, arrayIndex);              // ref long[] srcElement = ref src[arrayIndex];
                Expression srcTyped = null;
                if (fieldType == typeof(long) || fieldType == typeof(int) ||fieldType == typeof(short) || fieldType == typeof(byte)) {
                    srcTyped    = Expression.Convert(srcElement, field.fieldTypeNative);// srcTyped  = (<Field Type>)srcElement;
                } else if (fieldType == typeof(bool)) {
                    var not0    = Expression.NotEqual(srcElement, Expression.Constant(0L));
                    srcTyped    = Expression.Condition(not0, Expression.Constant(true), Expression.Constant(false)); // srcTyped   = srcElement != 0;
                } else if (fieldType == typeof(double)) {
                    
                } else if (fieldType == typeof(float)) {
                    
                }
                else
                    throw new InvalidOperationException("Unexpected primitive type: " + fieldType);
                
                 
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
