// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Transform.Query.Ops
{
    // ------------------------------------------- unary -------------------------------------------
    public abstract class UnaryAggregateOp : Operation
    {
        [Fri.Required]  public              Field       field;
        [Fri.Ignore]    internal  readonly  EvalResult  evalResult = new EvalResult(new List<Scalar> {new Scalar()});

        protected UnaryAggregateOp() { }
        protected UnaryAggregateOp(Field field) {
            this.field = field;

        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            field.Init(cx, InitFlags.ArrayField);
        }
    }
    
    public class Count : UnaryAggregateOp
    {
        public Count() { }
        public Count(Field field) : base(field) { }

        public override string      Linq => $"{field.Linq}.Count()";
        
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
        [Fri.Required]  public              Field       field;
        [Fri.Required]  public              string      arg;
        [Fri.Required]  public              Operation   array;
        [Fri.Ignore]    internal  readonly  EvalResult  evalResult = new EvalResult(new List<Scalar> {new Scalar()});

        protected BinaryAggregateOp() { }
        protected BinaryAggregateOp(Field field, string arg, Operation array) {
            this.field      = field;
            this.arg        = arg;
            this.array      = array;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            cx.lambdaArgs.Add(arg, field);
            field.Init(cx, InitFlags.ArrayField);
            array.Init(cx, flags);
        }
    }
    
    public class Min : BinaryAggregateOp
    {
        public Min() { }
        public Min(Field field, string arg, Operation array) : base(field, arg, array) { }

        public override string      Linq => $"{field.Linq}.Min({arg} => {array.Linq})";
        
        internal override EvalResult Eval(EvalCx cx) {
            Scalar currentMin = new Scalar();
            var eval = array.Eval(cx);
            foreach (var val in eval.values) {
                if (currentMin.type != ScalarType.Undefined) {
                    if (val.CompareTo(currentMin) < 0)
                        currentMin = val;
                } else {
                    currentMin = val;
                }
            }
            evalResult.SetSingle(currentMin);
            return evalResult;
        }
    }
    
    public class Max : BinaryAggregateOp
    {
        public Max() { }
        public Max(Field field, string arg, Operation array) : base(field, arg, array) { }

        public override string      Linq => $"{field.Linq}.Max({arg} => {array.Linq})";
        
        internal override EvalResult Eval(EvalCx cx) {
            Scalar currentMin = new Scalar();
            var eval = array.Eval(cx);
            foreach (var val in eval.values) {
                if (currentMin.type != ScalarType.Undefined) {
                    if (val.CompareTo(currentMin) > 0)
                        currentMin = val;
                } else {
                    currentMin = val;
                }
            }
            evalResult.SetSingle(currentMin);
            return evalResult;
        }
    }
    
    public class Sum : BinaryAggregateOp
    {
        public Sum() { }
        public Sum(Field field, string arg, Operation array) : base(field, arg, array) { }

        public override string      Linq => $"{field.Linq}.Sum({arg} => {array.Linq})";
        
        internal override EvalResult Eval(EvalCx cx) {
            Scalar sum = new Scalar(0);
            var eval = array.Eval(cx);
            foreach (var val in eval.values) {
                sum = sum.Add(val);
            }
            evalResult.SetSingle(sum);
            return evalResult;
        }
    }
    
    public class Average : BinaryAggregateOp
    {
        public Average() { }
        public Average(Field field, string arg, Operation array) : base(field, arg, array) { }

        public override string      Linq => $"{field.Linq}.Average({arg} => {array.Linq})";
        
        internal override EvalResult Eval(EvalCx cx) {
            Scalar sum = new Scalar(0);
            var eval = array.Eval(cx);
            int count = 0;
            foreach (var val in eval.values) {
                sum = sum.Add(val);
                count++;
            }
            var average = sum.Divide(new Scalar((double)count)); 
            evalResult.SetSingle(average);
            return evalResult;
        }
    }
    

}
