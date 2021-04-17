// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Graph.Query
{
    // ------------------------------------------- unary -------------------------------------------
    public abstract class UnaryAggregateOp : Operator
    {
        protected           Field               field;
        internal  readonly  EvalResult          evalResult = new EvalResult(new List<Scalar> {new Scalar()});

        protected UnaryAggregateOp(Field field) {
            this.field = field;

        }
        
        internal override void Init(OperatorContext cx) {
            cx.ValidateReuse(this); // results are reused
            field.Init(cx);
        }
    }
    
    public class Count : UnaryAggregateOp
    {
        public Count(Field field) : base(field) { }

        public override     string      ToString() => $"{field}.Count()";
        
        internal override EvalResult Eval(EvalCx cx) {
            var eval = field.Eval(cx);
            int count = eval.values.Count;
            evalResult.SetSingle(new Scalar(count));
            return evalResult;
        }
    }
    
    // ------------------------------------------- binary -------------------------------------------
    public abstract class BinaryAggregateOp : Operator
    {
        protected           Field               field;
        public              string              parameter;
        protected           Operator            array;
        internal  readonly  EvalResult          evalResult = new EvalResult(new List<Scalar> {new Scalar()});

        protected BinaryAggregateOp(Field field, string parameter, Operator array) {
            this.field      = field;
            this.parameter  = parameter;
            this.array      = array;
        }
        
        internal override void Init(OperatorContext cx) {
            cx.ValidateReuse(this); // results are reused
            cx.parameters.Add(parameter, field);
            field.Init(cx);
            array.Init(cx);
        }
    }
    
    public class Min : BinaryAggregateOp
    {
        public Min(Field field, string parameter, Operator array) : base(field, parameter, array) { }

        public override     string      ToString() => $"{field}.Min({parameter} => {array})";
        
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
        public Max(Field field, string parameter, Operator array) : base(field, parameter, array) { }

        public override     string      ToString() => $"{field}.Max({parameter} => {array})";
        
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
        public Sum(Field field, string parameter, Operator array) : base(field, parameter, array) { }

        public override     string      ToString() => $"{field}.Sum({parameter} => {array})";
        
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
        public Average(Field field, string parameter, Operator array) : base(field, parameter, array) { }

        public override     string      ToString() => $"{field}.Average({parameter} => {array})";
        
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
