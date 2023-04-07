// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBeProtected.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // ------------------------------------------- unary -------------------------------------------
    public abstract class UnaryAggregateOp : Operation
    {
        [Required]  public              Field       field;
        [Ignore]    internal  readonly  EvalResult  evalResult = new EvalResult(new List<Scalar> {new Scalar()});

        protected UnaryAggregateOp() { }
        protected UnaryAggregateOp(Field field) {
            this.field = field;

        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            field.Init(cx, InitFlags.ArrayField);
        }
    }
    
    public sealed class Count : UnaryAggregateOp
    {
        public Count() { }
        public Count(Field field) : base(field) { }

        public   override string    OperationName => "Count";
        public   override void      AppendLinq(AppendCx cx) { field.AppendLinq(cx); cx.sb.Append(".Count()"); }

        internal override EvalResult Eval(EvalCx cx) {
            var eval = field.Eval(cx);
            int count = eval.values.Count;
            evalResult.SetSingle(new Scalar(count));
            return evalResult;
        }
    }
    
    // ------------------------------------------- binary -------------------------------------------
    public abstract class BinaryAggregateOp : Operation
    {
        [Required]  public              Field       field;
        [Required]  public              string      arg;
        [Required]  public              Operation   array;
        [Ignore]    internal  readonly  EvalResult  evalResult = new EvalResult(new List<Scalar> {new Scalar()});

        protected BinaryAggregateOp() { }
        protected BinaryAggregateOp(Field field, string arg, Operation array) {
            this.field      = field;
            this.arg        = arg;
            this.array      = array;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            cx.variables.Add(arg, field);
            field.Init(cx, InitFlags.ArrayField);
            array.Init(cx, flags);
        }
    }
    
    public sealed class Min : BinaryAggregateOp
    {
        public Min() { }
        public Min(Field field, string arg, Operation array) : base(field, arg, array) { }

        public   override string    OperationName => "Min";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqArrow("Min", field, arg, array, cx);

        internal override EvalResult Eval(EvalCx cx) {
            Scalar currentMin = Null;
            var eval = array.Eval(cx);
            foreach (var val in eval.values) {
                if (!currentMin.IsNull) {
                    if (val.CompareTo(currentMin, array, out Scalar result) < 0)
                        currentMin = val;
                    if (result.IsError)
                        return evalResult.SetError(result);
                } else {
                    currentMin = val;
                }
            }
            evalResult.SetSingle(currentMin);
            return evalResult;
        }
    }
    
    public sealed class Max : BinaryAggregateOp
    {
        public Max() { }
        public Max(Field field, string arg, Operation array) : base(field, arg, array) { }

        public   override string    OperationName => "Max";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqArrow("Max", field, arg, array, cx);

        internal override EvalResult Eval(EvalCx cx) {
            Scalar currentMin = Null;
            var eval = array.Eval(cx);
            foreach (var val in eval.values) {
                if (!currentMin.IsNull) {
                    if (val.CompareTo(currentMin, array, out Scalar result) > 0)
                        currentMin = val;
                    if (result.IsError)
                        return evalResult.SetError(result);
                } else {
                    currentMin = val;
                }
            }
            evalResult.SetSingle(currentMin);
            return evalResult;
        }
    }
    
    public sealed class Sum : BinaryAggregateOp
    {
        public Sum() { }
        public Sum(Field field, string arg, Operation array) : base(field, arg, array) { }

        public   override string    OperationName => "Sum";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqArrow("Sum", field, arg, array, cx);
        
        internal override EvalResult Eval(EvalCx cx) {
            Scalar sum = Scalar.Zero;
            var eval = array.Eval(cx);
            foreach (var val in eval.values) {
                sum = sum.Add(val, this);
            }
            evalResult.SetSingle(sum);
            return evalResult;
        }
    }
    
    public sealed class Average : BinaryAggregateOp
    {
        public Average() { }
        public Average(Field field, string arg, Operation array) : base(field, arg, array) { }

        public   override string    OperationName => "Average";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqArrow("Average", field, arg, array, cx);

        internal override EvalResult Eval(EvalCx cx) {
            Scalar sum = Scalar.Zero;
            var eval = array.Eval(cx);
            int count = 0;
            foreach (var val in eval.values) {
                sum = sum.Add(val, this);
                count++;
            }
            var average = sum.Divide(new Scalar((double)count), this); 
            evalResult.SetSingle(average);
            return evalResult;
        }
    }
}
