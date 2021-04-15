// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Select;

namespace Friflo.Json.Flow.Graph.Query
{
    public abstract class UnaryAggregateOp : Operator
    {
        protected           Operator            array;
        internal  readonly  EvalResult          evalResult = new EvalResult(new List<Scalar> {new Scalar()});

        protected UnaryAggregateOp(Operator array) { this.array = array; }
        
        internal override void Init(OperatorContext cx) {
            cx.ValidateReuse(this); // results are reused
            array.Init(cx);
        }
    }
    
    public class Min : UnaryAggregateOp
    {
        public Min(Operator array) : base(array) { }

        public override     string      ToString() => $"Min({array})";
        
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
    
    public class Max : UnaryAggregateOp
    {
        public Max(Operator array) : base(array) { }

        public override     string      ToString() => $"Max({array})";
        
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
    
    public class Sum : UnaryAggregateOp
    {
        public Sum(Operator array) : base(array) { }

        public override     string      ToString() => $"Sum({array})";
        
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
    
    public class Average : UnaryAggregateOp
    {
        public Average(Operator array) : base(array) { }

        public override     string      ToString() => $"Average({array})";
        
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
    
    public class Count : UnaryAggregateOp
    {
        public Count(Operator array) : base(array) { }

        public override     string      ToString() => $"Count({array})";
        
        internal override EvalResult Eval(EvalCx cx) {
            var eval = array.Eval(cx);
            int count = eval.values.Count;
            evalResult.SetSingle(new Scalar(count));
            return evalResult;
        }
    }
}
