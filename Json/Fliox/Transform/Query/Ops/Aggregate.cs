// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBeProtected.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // ------------------------------------------- unary -------------------------------------------
    public abstract class UnaryAggregateOp : Operation
    {
        [Required]  public              Field       field;

        protected UnaryAggregateOp() { }
        protected UnaryAggregateOp(Field field) {
            this.field = field;

        }
        
        internal override void Init(OperationContext cx) {
            cx.ValidateReuse(this); // results are reused
            field.Init(cx);
        }
    }
    
    public sealed class Count : UnaryAggregateOp
    {
        public Count() { }
        public Count(Field field) : base(field) { }

        public   override string    OperationName   => "Count";
        public   override OpType    Type            => OpType.COUNT;
        internal override void      AppendLinq(AppendCx cx) { field.AppendLinq(cx); cx.sb.Append(".Count()"); }

        internal override Scalar Eval(EvalCx cx) {
            int count = cx.CountArray(field);
            return new Scalar (count);
        }
    }
    
    // ------------------------------------------- binary -------------------------------------------
    public abstract class BinaryAggregateOp : Operation
    {
        [Required]  public              Field       field;
        [Required]  public              string      arg;
        [Required]  public              Operation   array;

        protected BinaryAggregateOp() { }
        protected BinaryAggregateOp(Field field, string arg, Operation array) {
            this.field      = field;
            this.arg        = arg;
            this.array      = array;
        }
        
        internal override void Init(OperationContext cx) {
            cx.ValidateReuse(this); // results are reused
            cx.initArgs.Add(arg);
            field.Init(cx);
            array.Init(cx);
        }
    }
    
    public sealed class Min : BinaryAggregateOp
    {
        public Min() { }
        public Min(Field field, string arg, Operation array) : base(field, arg, array) { }

        public   override string    OperationName           => "Min";
        public   override OpType    Type                    => OpType.MIN;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqArrow("Min", field, arg, array, cx);

        internal override Scalar Eval(EvalCx cx) {
            using (cx.AddArrayArg(arg, field, out var item)) {
                var currentMin  = Null;
                while (item.HasNext()) {
                    var value = array.Eval(cx);
                    if (!currentMin.IsNull) {
                        if (value.CompareTo(currentMin, array, out Scalar result) < 0)
                            currentMin = value;
                        if (result.IsError)
                            return result;
                    } else {
                        currentMin = value;
                    }
                    item.MoveNext();
                }
                return currentMin;
            }
        }
    }
    
    public sealed class Max : BinaryAggregateOp
    {
        public Max() { }
        public Max(Field field, string arg, Operation array) : base(field, arg, array) { }

        public   override string    OperationName           => "Max";
        public   override OpType    Type                    => OpType.MAX;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqArrow("Max", field, arg, array, cx);

        internal override Scalar Eval(EvalCx cx) {
            using (cx.AddArrayArg(arg, field, out var item)) {
                Scalar currentMax = Null;
                while (item.HasNext()) {
                    var value = array.Eval(cx);
                    if (!currentMax.IsNull) {
                        if (value.CompareTo(currentMax, array, out Scalar result) > 0)
                            currentMax = value;
                        if (result.IsError)
                            return result;
                    } else {
                        currentMax = value;
                    }
                    item.MoveNext();
                }
                return currentMax;
            }
        }
    }
    
    public sealed class Sum : BinaryAggregateOp
    {
        public Sum() { }
        public Sum(Field field, string arg, Operation array) : base(field, arg, array) { }

        public   override string    OperationName           => "Sum";
        public   override OpType    Type                    => OpType.SUM;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqArrow("Sum", field, arg, array, cx);
        
        internal override Scalar Eval(EvalCx cx) {
            using (cx.AddArrayArg(arg, field, out var item)) {
                Scalar sum = Scalar.Zero;
                while (item.HasNext()) {
                    var value = array.Eval(cx);
                    sum = sum.Add(value, this);
                    item.MoveNext();
                }
                return sum;
            }
        }
    }
    
    public sealed class Average : BinaryAggregateOp
    {
        public Average() { }
        public Average(Field field, string arg, Operation array) : base(field, arg, array) { }

        public   override string    OperationName           => "Average";
        public   override OpType    Type                    => OpType.AVERAGE;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqArrow("Average", field, arg, array, cx);

        internal override Scalar Eval(EvalCx cx) {
            using (cx.AddArrayArg(arg, field, out var item)) {
                Scalar sum = Scalar.Zero;
                int count = 0;
                while (item.HasNext()) {
                    var value = array.Eval(cx);
                    sum = sum.Add(value, this);
                    count++;
                    item.MoveNext();
                }
                return sum.Divide(new Scalar((double)count), this); 
            }
        }
    }
}
