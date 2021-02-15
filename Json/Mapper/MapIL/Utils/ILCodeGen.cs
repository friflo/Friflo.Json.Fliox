// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.MapIL.Obj;
using Exp = System.Linq.Expressions.Expression;
    
namespace Friflo.Json.Mapper.MapIL.Utils
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
        internal static Expression<LoadObject<long?[], object[], T>> LoadInstanceExpression<T> (TypeMapper mapper) {
            var ctx = new LoadContext();
            ctx.dst         = Exp.Parameter(typeof(long?[]),            "dst");     // parameter: long[]   dst;
            ctx.dstObj      = Exp.Parameter(typeof(object[]),           "dstObj");  // parameter: object[] dstObj;
            
            var src         = Exp.Parameter(typeof(T).MakeByRefType(),  "src");     // parameter: object   src;

            AddLoadMembers(ctx, mapper, src);
            
            var assignmentsBlock= Exp.Block(ctx.assignmentList);
            
            var lambda = Exp.Lambda<LoadObject<long?[], object[], T>> (assignmentsBlock, ctx.dst, ctx.dstObj, src);
            return lambda;
        }

        private static readonly MethodInfo DoubleToInt64Bits = typeof(BitConverter).GetMethod(nameof(BitConverter.DoubleToInt64Bits));
        private static readonly MethodInfo SingleToInt32Bits = typeof(BitConverter).GetMethod(nameof(BitConverter.SingleToInt32Bits));

        private static void AddLoadMembers (LoadContext ctx, TypeMapper mapper, Expression srcTyped) {
            PropertyFields propFields = mapper.propFields;
            Type nullableStruct = TypeUtils.GetNullableStruct(srcTyped.Type);
            if (nullableStruct != null) {
                var value    = Exp.Field(srcTyped, "value");     // type of struct
                var hasValue = Exp.Field(srcTyped, "hasValue");  // type: bool
                
                // assign the state of hasValue
                var arrayIndex  = Exp.Constant(ctx.primIndex++, typeof(int));   // int arrayIndex = primIndex;
                var dstElement  = Exp.ArrayAccess(ctx.dst, arrayIndex);         // ref long dstElement = ref dst[arrayIndex];
                var val         = Exp.Condition(hasValue, Exp.Constant(1L, typeof(long?)), Exp.Constant(null, typeof(long?)), typeof(long?)); // longVal   = memberVal ? 1 : 0;
                var dstAssign   = Exp.Assign(dstElement, val);              // dstElement = longVal;
                ctx.assignmentList.Add(dstAssign);
                AddLoadMembers(ctx, mapper, value);
                return;
            }

            for (int n = 0; n < propFields.fields.Length; n++) {
                PropField field         = propFields.fields[n];
                Type fieldType          = field.fieldType.type;
                Type ut                 = field.fieldType.underlyingType;


                MemberExpression memberVal;
                if (field.field != null)
                    memberVal = Exp.Field   (srcTyped, field.name); // memberVal = srcTyped.<field.name>;
                else
                    memberVal = Exp.Property(srcTyped, field.name); // memberVal = srcTyped.<field.name>;

                BinaryExpression dstAssign;
                if (!fieldType.IsPrimitive && !field.fieldType.isValueType) {
                    // --- object field
                    var arrObjIndex = Exp.Constant(ctx.objIndex++, typeof(int));    // int arrObjIndex = objIndex;
                    var dstElement  = Exp.ArrayAccess(ctx.dstObj, arrObjIndex);     // ref object dstElement = ref dstObj[arrObjIndex];
                    var objVal      = Exp.Convert(memberVal, typeof(object));       // box
                    dstAssign       = Exp.Assign(dstElement, objVal);            // dstElement = memberVal;
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
                        }  else if (fieldType == typeof(bool?)) {
                            var isNull  = Exp.Equal(memberVal, Exp.Constant(null, typeof(bool?)));
                            var bln     = Exp.Convert(memberVal, typeof(bool));
                            var val     = Exp.Condition(bln, Exp.Constant(1L, typeof(long?)), Exp.Constant(0L, typeof(long?))); // longVal   = memberVal ? 1 : 0;
                            longVal     = Exp.Condition(isNull, Exp.Constant(null, typeof(long?)), val);
                        } else if (fieldType == typeof(double)) {
                            // ReSharper disable once AssignNullToNotNullAttribute
                            var val     = Exp.Call(DoubleToInt64Bits, memberVal);       // dbVal = BitConverter.DoubleToInt64Bits(memberVal);
                            longVal     = Exp.Convert(val, typeof(long?));            // longVal = (long)dbVal;
                        } else if (fieldType == typeof(double?)) {
                            var isNull  = Exp.Equal(memberVal, Exp.Constant(null, typeof(double?)));
                            var dbl     = Exp.Convert(memberVal, typeof(double)); 
                            var val     = Exp.Call(DoubleToInt64Bits, dbl);           // dbVal = BitConverter.DoubleToInt64Bits(memberVal);
                            var longConv= Exp.Convert(val, typeof(long?));            // longVal = (long)dbVal;
                            longVal     = Exp.Condition(isNull, Exp.Constant(null, typeof(long?)), longConv);
                        } else if (fieldType == typeof(float)) {
                            // ReSharper disable once AssignNullToNotNullAttribute
                            var intVal  = Exp.Call(SingleToInt32Bits, memberVal);       // intVal  = BitConverter.SingleToInt32Bits(memberVal);
                            longVal     = Exp.Convert(intVal, typeof(long?));            // longVal = (long)intVal;
                        } else if (fieldType == typeof(float?)) {
                            var isNull  = Exp.Equal(memberVal, Exp.Constant(null, typeof(float?)));
                            var flt     = Exp.Convert(memberVal, typeof(float)); 
                            var val     = Exp.Call(SingleToInt32Bits, flt);           // dbVal = BitConverter.DoubleToInt64Bits(memberVal);
                            var longConv= Exp.Convert(val, typeof(long?));            // longVal = (long)dbVal;
                            longVal     = Exp.Condition(isNull, Exp.Constant(null, typeof(long?)), longConv);
                        } else
                            throw new InvalidOperationException("Unexpected primitive type: " + fieldType);

                        var arrayIndex  = Exp.Constant(ctx.primIndex++, typeof(int));   // int arrayIndex = primIndex;
                        var dstElement  = Exp.ArrayAccess(ctx.dst, arrayIndex);         // ref long dstElement = ref dst[arrayIndex];
                        dstAssign       = Exp.Assign(dstElement, longVal);              // dstElement = longVal;
                    } else if (fieldType.IsEnum || ut != null && ut.IsEnum) {
                        // var underlyingEnumType = Enum.GetUnderlyingType(fieldType);
                        var arrayIndex  = Exp.Constant(ctx.primIndex++, typeof(int));  
                        var dstElement  = Exp.ArrayAccess(ctx.dst, arrayIndex);
                        var longVal     = Exp.Convert(memberVal, typeof(long?));       // longVal   = (long)memberVal;
                        dstAssign       = Exp.Assign(dstElement, longVal);
                    } else {
                        // --- struct field
                        AddLoadMembers(ctx, field.fieldType, memberVal);
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

        internal static Expression<StoreObject<T, long?[], object[]>> StoreInstanceExpression<T> (TypeMapper mapper) {
            var ctx = new StoreContext();
            var dst         = Exp.Parameter(typeof(T).MakeByRefType(),  "dst");       // parameter: long[]   dst;
            ctx.src         = Exp.Parameter(typeof(long?[]),            "src");       // parameter: object   src;
            ctx.srcObj      = Exp.Parameter(typeof(object[]),           "srcObj");    // parameter: object[] srcObj;
            
            AddStoreMembers(ctx, mapper, dst);
            
            var assignmentsBlock= Exp.Block(ctx.assignmentList);
            var lambda = Exp.Lambda<StoreObject<T, long?[], object[]>> (assignmentsBlock, dst, ctx.src, ctx.srcObj);
            return lambda;
        }
        
        private static readonly MethodInfo Int64BitsToDouble = typeof(BitConverter).GetMethod(nameof(BitConverter.Int64BitsToDouble));
        private static readonly MethodInfo Int32BitsToSingle = typeof(BitConverter).GetMethod(nameof(BitConverter.Int32BitsToSingle));

        private static void AddStoreMembers (StoreContext ctx, TypeMapper mapper, Expression dstTyped) {
            PropertyFields propFields = mapper.propFields;
            Type nullableStruct = TypeUtils.GetNullableStruct(dstTyped.Type);
            if (nullableStruct != null) {
                var value       = Exp.Field(dstTyped, "value");    // type of struct
                
                var arrayIndex  = Exp.Constant(ctx.primIndex++, typeof(int));   // int arrayIndex = primIndex;
                var srcElement  = Exp.ArrayAccess(ctx.src, arrayIndex);         // ref long srcElement = ref src[arrayIndex];
                var notNull     = Exp.NotEqual(srcElement, Exp.Constant(null, typeof(long?)));
                // instantiate new struct and assign to Nullable<>, if src array element is not null
                var newStruct   = Exp.New(nullableStruct); // , new Expression[0]);
                var newNullable = Exp.Convert(newStruct, dstTyped.Type);
                var newValue    = Exp.Condition(notNull, newNullable, Exp.Constant(null, dstTyped.Type), dstTyped.Type); // srcTyped = srcElement != 0;
                var dstAssign   = Exp.Assign(dstTyped, newValue);              // dstMember = srcTyped;
                ctx.assignmentList.Add(dstAssign);
                AddStoreMembers(ctx, mapper, value);
                return;
            }
            
            for (int n = 0; n < propFields.fields.Length; n++) {
                PropField field = propFields.fields[n];
                Type fieldType  = field.fieldType.type;
                Type ut         = field.fieldType.underlyingType;
                
                MemberExpression dstMember;
                if (field.field != null)
                    dstMember   = Exp.Field   (dstTyped, field.name);               // ref dstMember = ref dstTyped.<field.name>;
                else
                    dstMember   = Exp.Property(dstTyped, field.name);               // ref dstMember = ref dstTyped.<field.name>;
                
                BinaryExpression dstAssign;
                if (!fieldType.IsPrimitive && !field.fieldType.isValueType) {
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
                        }  else if (fieldType == typeof(bool?)) {
                            var isNull  = Exp.Equal(srcElement, Exp.Constant(null, typeof(bool?)));
                            var lngVal  = Exp.Convert(srcElement, typeof(long?));
                            var not0    = Exp.NotEqual(lngVal, Exp.Constant(0L, typeof(long?)));
                            var bln     = Exp.Condition(not0, Exp.Constant(true, typeof(bool?)), Exp.Constant(false, typeof(bool?)));
                            srcTyped    = Exp.Condition(isNull, Exp.Constant(null, typeof(bool?)), bln);
                        } else if (fieldType == typeof(double)) {
                            var lngVal  = Exp.Convert(srcElement, typeof(long));
                            // ReSharper disable once AssignNullToNotNullAttribute
                            srcTyped    = Exp.Call(Int64BitsToDouble, lngVal);          // srcTyped = BitConverter.Int64BitsToDouble (srcElement);
                        } else if (fieldType == typeof(double?)) {
                            var isNull  = Exp.Equal(srcElement, Exp.Constant(null, typeof(double?)));
                            var lngVal  = Exp.Convert(srcElement, typeof(long));
                            // ReSharper disable once AssignNullToNotNullAttribute
                            var val     = Exp.Call(Int64BitsToDouble, lngVal);          // srcTyped = BitConverter.Int64BitsToDouble (srcElement);
                            var valConv = Exp.Convert(val, typeof(double?));
                            srcTyped    = Exp.Condition(isNull, Exp.Constant(null, typeof(double?)), valConv);
                        } else if (fieldType == typeof(float)) {
                            var srcInt  = Exp.Convert(srcElement, typeof(int));         // srcInt   = (int)srcElement;
                            // ReSharper disable once AssignNullToNotNullAttribute
                            srcTyped    = Exp.Call(Int32BitsToSingle, srcInt);          // srcTyped = BitConverter.Int32BitsToSingle (srcInt);
                        } else if (fieldType == typeof(float?)) {
                            var isNull  = Exp.Equal(srcElement, Exp.Constant(null, typeof(float?)));
                            var intVal  = Exp.Convert(srcElement, typeof(int));
                            // ReSharper disable once AssignNullToNotNullAttribute
                            var val     = Exp.Call(Int32BitsToSingle, intVal);          // srcTyped = BitConverter.Int64BitsToDouble (srcElement);
                            var valConv = Exp.Convert(val, typeof(float?));
                            srcTyped    = Exp.Condition(isNull, Exp.Constant(null, typeof(float?)), valConv);
                        }
                        else
                            throw new InvalidOperationException("Unexpected primitive type: " + fieldType);

                        dstAssign       = Exp.Assign(dstMember, srcTyped);              // dstMember = srcTyped;
                    } else if (fieldType.IsEnum || ut != null && ut.IsEnum) {
                        var arrayIndex  = Exp.Constant(ctx.primIndex++, typeof(int));   // int arrayIndex = primIndex;
                        var srcElement  = Exp.ArrayAccess(ctx.src, arrayIndex);         // ref long srcElement = ref src[arrayIndex];
                        var srcTyped    = Exp.Convert(srcElement, fieldType); 
                        dstAssign       = Exp.Assign(dstMember, srcTyped);
                    } else {
                        // --- struct field
                        AddStoreMembers(ctx, field.fieldType, dstMember);
                        continue; // struct itself is not assigned - only its members
                    }
                }
                ctx.assignmentList.Add(dstAssign);
            }

        }
    }
}

#endif
