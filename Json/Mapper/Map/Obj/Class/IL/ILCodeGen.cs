// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;
using Exp = System.Linq.Expressions.Expression;
    
namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{
    public static class ILCodeGen
    {
        class LoadContext {
            internal            ParameterExpression    dst;
            internal            ParameterExpression    dstObj;

            internal readonly   List<BinaryExpression> assignmentList = new List<BinaryExpression>();
            
            internal            int                    primIndex;
            internal            int                    objIndex;
        }

        // Nice Blog about expression trees:
        // [Working with Expression Trees in C# | Alexey Golub] https://tyrrrz.me/blog/expression-trees
        //
        // The idea of loading/storing all fields of a class with one method call came to this nice blog:
        // [Optimizing reflection in C# via dynamic code generation | by Sergio Pedri | Medium]
        // https://medium.com/@SergioPedri/optimizing-reflection-with-dynamic-code-generation-6e15cef4b1a2
        internal static Expression<Action<long?[], object[], T>> LoadInstanceExpression<T> (PropertyFields propFields) {
            var ctx = new LoadContext();
            ctx.dst         = Exp.Parameter(typeof(long?[]),  "dst");     // parameter: long[]   dst;
            ctx.dstObj      = Exp.Parameter(typeof(object[]), "dstObj");  // parameter: object[] dstObj;
            
            var src         = Exp.Parameter(typeof(T),        "src");     // parameter: object   src;

            AddLoadMembers(ctx, propFields, src);
            
            var assignmentsBlock= Exp.Block(ctx.assignmentList);
            
            var lambda = Exp.Lambda<Action<long?[], object[], T>> (assignmentsBlock, ctx.dst, ctx.dstObj, src);
            return lambda;
        }

        private static readonly MethodInfo DoubleToInt64Bits = typeof(BitConverter).GetMethod(nameof(BitConverter.DoubleToInt64Bits));
        private static readonly MethodInfo SingleToInt32Bits = typeof(BitConverter).GetMethod(nameof(BitConverter.SingleToInt32Bits));

        private static void AddLoadMembers (LoadContext ctx, PropertyFields propFields, Expression srcTyped) {

            for (int n = 0; n < propFields.fields.Length; n++) {
                PropField field = propFields.fields[n];
                Type fieldType  = field.fieldTypeNative;
                Type ut         = field.fieldType.underlyingType;
                
                MemberExpression memberVal;
                if (field.field != null)
                    memberVal   = Exp.Field(   srcTyped, field.name);           // memberVal = srcTyped.<field.name>;
                else
                    memberVal   = Exp.Property(srcTyped, field.name);           // memberVal = srcTyped.<field.name>;

                BinaryExpression dstAssign;
                if (!fieldType.IsPrimitive && !fieldType.IsValueType) {
                    // --- object field
                    var arrObjIndex = Exp.Constant(ctx.objIndex++, typeof(int));    // int arrObjIndex = objIndex;
                    var dstElement  = Exp.ArrayAccess(ctx.dstObj, arrObjIndex);     // ref object dstElement = ref dstObj[arrObjIndex];
                    dstAssign       = Exp.Assign(dstElement, memberVal);            // dstElement = memberVal;
                } else {
                    if (fieldType.IsPrimitive || ut != null && ut.IsPrimitive) {
                        // --- primitive field
                        Expression longVal;
                        if (fieldType == typeof(long)  || fieldType == typeof(int)  || fieldType == typeof(short)  || fieldType == typeof(byte) ||
                            fieldType == typeof(long?) || fieldType == typeof(int?) || fieldType == typeof(short?) || fieldType == typeof(byte?)) {
                            longVal     = Exp.Convert(memberVal, typeof(long?));      // longVal   = (long)memberVal;
                        } else if (fieldType == typeof(bool)) {
                            var val     = Exp.Condition(memberVal, Exp.Constant(1L), Exp.Constant(0L)); // longVal   = memberVal ? 1 : 0;
                            longVal     = Exp.Convert(val, typeof(long?));            // longVal = (long)dbVal;
                        } else if (fieldType == typeof(double)) {
                            // ReSharper disable once AssignNullToNotNullAttribute
                            var val     = Exp.Call(DoubleToInt64Bits, memberVal);       // dbVal = BitConverter.DoubleToInt64Bits(memberVal);
                            longVal     = Exp.Convert(val, typeof(long?));            // longVal = (long)dbVal;
                        } else if (fieldType == typeof(float)) {
                            // ReSharper disable once AssignNullToNotNullAttribute
                            var intVal  = Exp.Call(SingleToInt32Bits, memberVal);       // intVal  = BitConverter.SingleToInt32Bits(memberVal);
                            longVal     = Exp.Convert(intVal, typeof(long?));            // longVal = (long)intVal;
                        } else
                            throw new InvalidOperationException("Unexpected primitive type: " + fieldType);

                        var arrayIndex  = Exp.Constant(ctx.primIndex++, typeof(int));   // int arrayIndex = primIndex;
                        var dstElement  = Exp.ArrayAccess(ctx.dst, arrayIndex);         // ref long dstElement = ref dst[arrayIndex];
                        dstAssign       = Exp.Assign(dstElement, longVal);              // dstElement = longVal;
                    } else {
                        // --- struct field
                        AddLoadMembers(ctx, field.fieldType.GetPropFields(), memberVal);
                        continue; // struct itself is not assigned - only its members
                    }
                }
                ctx.assignmentList.Add(dstAssign);
            }
        }
        
        class StoreContext {
            internal            ParameterExpression    src;
            internal            ParameterExpression    srcObj;
            
            internal readonly   List<BinaryExpression> assignmentList = new List<BinaryExpression>();
            
            internal            int                    primIndex;
            internal            int                    objIndex;
        }

        internal static Expression<Action<T, long?[], object[]>> StoreInstanceExpression<T> (PropertyFields propFields) {
            var ctx = new StoreContext();
            var dst         = Exp.Parameter(typeof(T),        "dst");       // parameter: long[]   dst;
            ctx.src         = Exp.Parameter(typeof(long?[]),  "src");       // parameter: object   src;
            ctx.srcObj      = Exp.Parameter(typeof(object[]), "srcObj");    // parameter: object[] srcObj;
            
            AddStoreMembers(ctx, propFields, dst);
            
            var assignmentsBlock= Exp.Block(ctx.assignmentList);
            var lambda = Exp.Lambda<Action<T, long?[], object[]>> (assignmentsBlock, dst, ctx.src, ctx.srcObj);
            return lambda;
        }
        
        private static readonly MethodInfo Int64BitsToDouble = typeof(BitConverter).GetMethod(nameof(BitConverter.Int64BitsToDouble));
        private static readonly MethodInfo Int32BitsToSingle = typeof(BitConverter).GetMethod(nameof(BitConverter.Int32BitsToSingle));

        private static void AddStoreMembers (StoreContext ctx, PropertyFields propFields, Expression dstTyped) {
            
            for (int n = 0; n < propFields.fields.Length; n++) {
                PropField field = propFields.fields[n];
                Type fieldType  = field.fieldTypeNative;
                Type ut         = field.fieldType.underlyingType;
                
                MemberExpression dstMember;
                if (field.field != null)
                    dstMember   = Exp.Field   (dstTyped, field.name);               // ref dstMember = ref dstTyped.<field.name>;
                else
                    dstMember   = Exp.Property(dstTyped, field.name);               // ref dstMember = ref dstTyped.<field.name>;
                
                BinaryExpression dstAssign;
                if (!fieldType.IsPrimitive && !fieldType.IsValueType) {
                    // --- object field
                    var arrayIndex  = Exp.Constant(ctx.objIndex++, typeof(int));    // int arrayIndex = objIndex;
                    var srcElement  = Exp.ArrayAccess(ctx.srcObj, arrayIndex);      // ref object srcElement = ref srcObj[arrayIndex];
                    var srcTyped    = Exp.Convert(srcElement, fieldType);           // <fieldType>srcTyped = (<fieldType>)srcElement;
                    dstAssign       = Exp.Assign(dstMember, srcTyped);              // dstMember = srcTyped;
                } else {
                    if (fieldType.IsPrimitive || ut != null && ut.IsPrimitive) {
                        // --- primitive field
                        var arrayIndex  = Exp.Constant(ctx.primIndex++, typeof(int));   // int arrayIndex = primIndex;
                        var srcElement  = Exp.ArrayAccess(ctx.src, arrayIndex);         // ref long srcElement = ref src[arrayIndex];
                        Expression srcTyped;
                        if (fieldType == typeof(long)  || fieldType == typeof(int)  || fieldType == typeof(short)  || fieldType == typeof(byte) ||
                            fieldType == typeof(long?) || fieldType == typeof(int?) || fieldType == typeof(short?) || fieldType == typeof(byte?)) {
                            srcTyped    = Exp.Convert(srcElement, fieldType);           // srcTyped  = (<Field Type>)srcElement;
                        } else if (fieldType == typeof(bool)) {
                            var lngVal  = Exp.Convert(srcElement, typeof(long));
                            var not0    = Exp.NotEqual(lngVal, Exp.Constant(0L));
                            srcTyped    = Exp.Condition(not0, Exp.Constant(true), Exp.Constant(false)); // srcTyped = srcElement != 0;
                        } else if (fieldType == typeof(double)) {
                            var lngVal  = Exp.Convert(srcElement, typeof(long));
                            // ReSharper disable once AssignNullToNotNullAttribute
                            srcTyped    = Exp.Call(Int64BitsToDouble, lngVal);          // srcTyped = BitConverter.Int64BitsToDouble (srcElement);
                        } else if (fieldType == typeof(float)) {
                            var srcInt  = Exp.Convert(srcElement, typeof(int));         // srcInt   = (int)srcElement;
                            // ReSharper disable once AssignNullToNotNullAttribute
                            srcTyped    = Exp.Call(Int32BitsToSingle, srcInt);          // srcTyped = BitConverter.Int32BitsToSingle (srcInt);
                        }
                        else
                            throw new InvalidOperationException("Unexpected primitive type: " + fieldType);

                        dstAssign       = Exp.Assign(dstMember, srcTyped);              // dstMember = srcTyped;
                    } else {
                        // --- struct field
                        AddStoreMembers(ctx, field.fieldType.GetPropFields(), dstMember);
                        continue; // struct itself is not assigned - only its members
                    }
                }
                ctx.assignmentList.Add(dstAssign);
            }

        }
    }
}

#endif
