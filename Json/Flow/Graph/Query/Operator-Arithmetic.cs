// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Query.Arity;
using Friflo.Json.Flow.Graph.Select;

namespace Friflo.Json.Flow.Graph.Query
{
    // ------------------------------------ unary arithmetic operators ------------------------------------
    public abstract class UnaryArithmeticOp : Operator
    {
        protected           Operator            operand;
        internal  readonly  EvalResult          evalResult = new EvalResult(new List<Scalar>());

        protected UnaryArithmeticOp(Operator operand) { this.operand = operand; }
        
        internal override void Init(OperatorContext cx) {
            cx.ValidateReuse(this); // results are reused
            operand.Init(cx);
        }
    }
    
    public class Abs : UnaryArithmeticOp
    {
        public Abs(Operator operand) : base(operand) { }

        public override     string      ToString() => $"Abs({operand})";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = operand.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Abs();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Ceiling : UnaryArithmeticOp
    {
        public Ceiling(Operator operand) : base(operand) { }

        public override     string      ToString() => $"Ceiling({operand})";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = operand.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Ceiling();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Floor : UnaryArithmeticOp
    {
        public Floor(Operator operand) : base(operand) { }

        public override     string      ToString() => $"Floor({operand})";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = operand.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Floor();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Exp : UnaryArithmeticOp
    {
        public Exp(Operator operand) : base(operand) { }

        public override     string      ToString() => $"Exp({operand})";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = operand.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Exp();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Log : UnaryArithmeticOp
    {
        public Log(Operator operand) : base(operand) { }

        public override     string      ToString() => $"Log({operand})";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = operand.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Log();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Sqrt : UnaryArithmeticOp
    {
        public Sqrt(Operator operand) : base(operand) { }

        public override     string      ToString() => $"Sqrt({operand})";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = operand.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Sqrt();
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    
    
    
    // ------------------------------------ binary arithmetic operators ------------------------------------
    public abstract class BinaryArithmeticOp : Operator
    {
        protected           Operator    left;
        protected           Operator    right;
        internal readonly   EvalResult  evalResult = new EvalResult(new List<Scalar>());
        
        protected BinaryArithmeticOp(Operator left, Operator right) {
            this.left = left;
            this.right = right;
        }
        
        internal override void Init(OperatorContext cx) {
            cx.ValidateReuse(this); // results are reused
            left.Init(cx);
            right.Init(cx);
        }
    }
    
    public class Add : BinaryArithmeticOp
    {
        public Add(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} + {right}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx), cx);
            foreach (var pair in eval) {
                var result = pair.left.Add(pair.right);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Subtract : BinaryArithmeticOp
    {
        public Subtract(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} - {right}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx), cx);
            foreach (var pair in eval) {
                var result = pair.left.Subtract(pair.right);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Multiply : BinaryArithmeticOp
    {
        public Multiply(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} * {right}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx), cx);
            foreach (var pair in eval) {
                var result = pair.left.Multiply(pair.right);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public class Divide : BinaryArithmeticOp
    {
        public Divide(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} / {right}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx), cx);
            foreach (var pair in eval) {
                var result = pair.left.Divide(pair.right);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }

}