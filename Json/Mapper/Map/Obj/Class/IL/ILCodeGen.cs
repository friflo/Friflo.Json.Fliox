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
        class LoadContext {
            internal            ParameterExpression    dst;
            internal            ParameterExpression    dstObj;
            internal            ParameterExpression    src;
            internal            UnaryExpression        srcTyped;
            internal readonly   List<BinaryExpression> assignmentList = new List<BinaryExpression>();
            internal            PropertyFields         propFields;
            
            internal            int                    primIndex;
            internal            int                    objIndex;
        }

        // Nice Blog about expression trees:
        // [Working with Expression Trees in C# | Alexey Golub] https://tyrrrz.me/blog/expression-trees
        //
        // The idea of loading/storing all fields of a class with one method call came to this nice blog:
        // [Optimizing reflection in C# via dynamic code generation | by Sergio Pedri | Medium]
        // https://medium.com/@SergioPedri/optimizing-reflection-with-dynamic-code-generation-6e15cef4b1a2
        internal static Expression<Action<long[], object[], object>> LoadInstanceExpression (PropertyFields propFields, Type type) {
            var ctx = new LoadContext();
            ctx.propFields  = propFields;
            ctx.dst         = Exp.Parameter(typeof(long[]),   "dst");     // parameter: long[]   dst;
            ctx.dstObj      = Exp.Parameter(typeof(object[]), "dstObj");  // parameter: object[] dstObj;
            ctx.src         = Exp.Parameter(typeof(object),   "src");     // parameter: object   src;
            
            ctx.srcTyped    = Exp.Convert(ctx.src, type);                 // <Type> srcTyped = (<Type>)src;
            
            AddLoadMembers(ctx);
            
            var assignmentsBlock= Exp.Block(ctx.assignmentList);
            
            var lambda = Exp.Lambda<Action<long[], object[], object>> (assignmentsBlock, ctx.dst, ctx.dstObj, ctx.src);
            return lambda;
        }
        
        private static void AddLoadMembers (LoadContext ctx) {
            
            var doubleToInt64Bits = typeof(BitConverter).GetMethod(nameof(BitConverter.DoubleToInt64Bits));
            var singleToInt32Bits = typeof(BitConverter).GetMethod(nameof(BitConverter.SingleToInt32Bits));
            
            for (int n = 0; n < ctx.propFields.fields.Length; n++) {
                PropField field = ctx.propFields.fields[n];
                Type fieldType  = field.fieldTypeNative;
                
                MemberExpression memberVal;
                if (field.field != null)
                    memberVal   = Exp.Field(   ctx.srcTyped, field.name);           // memberVal = srcTyped.<field.name>;
                else
                    memberVal   = Exp.Property(ctx.srcTyped, field.name);           // memberVal = srcTyped.<field.name>;

                BinaryExpression dstAssign;
                if (!fieldType.IsPrimitive && !fieldType.IsValueType) {
                    // --- object field
                    var arrObjIndex = Exp.Constant(ctx.objIndex++, typeof(int));    // int arrObjIndex = objIndex;
                    var dstElement  = Exp.ArrayAccess(ctx.dstObj, arrObjIndex);     // ref object dstElement = ref dstObj[arrObjIndex];
                    dstAssign       = Exp.Assign(dstElement, memberVal);            // dstElement = memberVal;
                } else {
                    if (fieldType.IsPrimitive) {
                        // --- primitive field
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

                        var arrayIndex  = Exp.Constant(ctx.primIndex++, typeof(int));   // int arrayIndex = primIndex;
                        var dstElement  = Exp.ArrayAccess(ctx.dst, arrayIndex);         // ref long dstElement = ref dst[arrayIndex];
                        dstAssign       = Exp.Assign(dstElement, longVal);              // dstElement = longVal;
                    } else {
                        // --- struct field
                        dstAssign = null;
                    }
                }
                ctx.assignmentList.Add(dstAssign);
            }
        }
        
        class StoreContext {
            internal            ParameterExpression    dst;
            internal            ParameterExpression    src;
            internal            ParameterExpression    srcObj;
            internal            UnaryExpression        dstTyped;
            internal readonly   List<BinaryExpression> assignmentList = new List<BinaryExpression>();
            internal            PropertyFields         propFields;
            
            internal            int                    primIndex;
            internal            int                    objIndex;
        }

        internal static Expression<Action<object, long[], object[]>> StoreInstanceExpression (PropertyFields propFields, Type type) {
            var ctx = new StoreContext();
            ctx.propFields  = propFields;
            ctx.dst         = Exp.Parameter(typeof(object),   "dst");       // parameter: long[]   dst;
            ctx.src         = Exp.Parameter(typeof(long[]),   "src");       // parameter: object   src;
            ctx.srcObj      = Exp.Parameter(typeof(object[]), "srcObj");    // parameter: object[] srcObj;
            
            ctx.dstTyped    = Exp.Convert(ctx.dst, type);                   // <Type> dstTyped = (<Type>)dst;
            
            AddStoreMembers(ctx);
            
            var assignmentsBlock= Exp.Block(ctx.assignmentList);
            var lambda = Exp.Lambda<Action<object, long[], object[]>> (assignmentsBlock, ctx.dst, ctx.src, ctx.srcObj);
            return lambda;
        }
            
        private static void AddStoreMembers (StoreContext ctx) {
            var int64BitsToDouble = typeof(BitConverter).GetMethod(nameof(BitConverter.Int64BitsToDouble));
            var int32BitsToSingle = typeof(BitConverter).GetMethod(nameof(BitConverter.Int32BitsToSingle));
            
            for (int n = 0; n < ctx.propFields.fields.Length; n++) {
                PropField field = ctx.propFields.fields[n];
                Type fieldType  = field.fieldTypeNative;
                
                MemberExpression dstMember;
                if (field.field != null)
                    dstMember   = Exp.Field   (ctx.dstTyped, field.name);           // ref dstMember = ref dstTyped.<field.name>;
                else
                    dstMember   = Exp.Property(ctx.dstTyped, field.name);           // ref dstMember = ref dstTyped.<field.name>;
                
                BinaryExpression dstAssign;
                if (!fieldType.IsPrimitive) {
                    // --- object field
                    var arrayIndex  = Exp.Constant(ctx.objIndex++, typeof(int));    // int arrayIndex = objIndex;
                    var srcElement  = Exp.ArrayAccess(ctx.srcObj, arrayIndex);      // ref object srcElement = ref srcObj[arrayIndex];
                    var srcTyped    = Exp.Convert(srcElement, fieldType);           // <fieldType>srcTyped = (<fieldType>)srcElement;
                    dstAssign       = Exp.Assign(dstMember, srcTyped);              // dstMember = srcTyped;
                } else {
                    // --- primitive field
                    var arrayIndex  = Exp.Constant(ctx.primIndex++, typeof(int));   // int arrayIndex = primIndex;
                    var srcElement  = Exp.ArrayAccess(ctx.src, arrayIndex);         // ref long srcElement = ref src[arrayIndex];
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

                    dstAssign       = Exp.Assign(dstMember, srcTyped);              // dstMember = srcTyped;
                }
                ctx.assignmentList.Add(dstAssign);
            }

        }
    }
}

#endif
