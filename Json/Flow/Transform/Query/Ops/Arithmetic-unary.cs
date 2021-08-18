// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Transform.Query.Ops
{
    // ------------------------------------ unary arithmetic operations ------------------------------------
    public abstract class UnaryArithmeticOp : Operation
    {
        [Fri.Required] public           Operation   value;
        [Fri.Ignore] internal  readonly EvalResult  evalResult = new EvalResult(new List<Scalar>());

        protected UnaryArithmeticOp() { }

        protected UnaryArithmeticOp(Operation value) {
            this.value = value;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            value.Init(cx, 0);
        }
    }
    
    public class Abs : UnaryArithmeticOp
    {
        public Abs() { }
        public Abs(Operation value) : base(value) { }

        public override string      Linq => $"Abs({value.Linq})";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Abs();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Ceiling : UnaryArithmeticOp
    {
        public Ceiling() { }
        public Ceiling(Operation value) : base(value) { }

        public override string      Linq => $"Ceiling({value.Linq})";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Ceiling();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Floor : UnaryArithmeticOp
    {
        public Floor() { }
        public Floor(Operation value) : base(value) { }

        public override string      Linq => $"Floor({value.Linq})";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Floor();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Exp : UnaryArithmeticOp
    {
        public Exp() { }
        public Exp(Operation value) : base(value) { }

        public override string      Linq => $"Exp({value.Linq})";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Exp();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Log : UnaryArithmeticOp
    {
        public Log() { }
        public Log(Operation value) : base(value) { }

        public override string      Linq => $"Log({value.Linq})";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Log();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Sqrt : UnaryArithmeticOp
    {
        public Sqrt() { }
        public Sqrt(Operation value) : base(value) { }

        public override string      Linq => $"Sqrt({value.Linq})";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Sqrt();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Negate : UnaryArithmeticOp
    {
        public Negate() { }
        public Negate(Operation value) : base(value) { }

        public override string      Linq => $"-({value.Linq})";
        
        internal override EvalResult Eval(EvalCx cx) {
            var zero = new Scalar(0);
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = zero.Subtract(val);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
}
