// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Friflo.Json.EntityGraph.Filter.Arity;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    // ------------------------------------ unary arithmetic operators ------------------------------------
    public abstract class UnaryArithmeticOp : Operator
    {
        protected           Operator            operand;
        internal  readonly  EvalResult          results = new EvalResult(new List<SelectorValue>());

        protected UnaryArithmeticOp(Operator operand) { this.operand = operand; }
        
        internal override void Init(GraphOpContext cx) {
            cx.ValidateReuse(this); // results are reused
            operand.Init(cx);
        }
    }
    
    public class Abs : UnaryArithmeticOp
    {
        public Abs(Operator operand) : base(operand) { }

        public override     string      ToString() => $"Abs({operand})";
        
        internal override EvalResult Eval() {
            results.Clear();
            var eval = operand.Eval();
            foreach (var val in eval.values) {
                var result = val.Abs();
                results.Add(result);
            }
            return results;
        }
    }
    
    public class Ceiling : UnaryArithmeticOp
    {
        public Ceiling(Operator operand) : base(operand) { }

        public override     string      ToString() => $"Ceiling({operand})";
        
        internal override EvalResult Eval() {
            results.Clear();
            var eval = operand.Eval();
            foreach (var val in eval.values) {
                var result = val.Ceiling();
                results.Add(result);
            }
            return results;
        }
    }
    
    public class Floor : UnaryArithmeticOp
    {
        public Floor(Operator operand) : base(operand) { }

        public override     string      ToString() => $"Floor({operand})";
        
        internal override EvalResult Eval() {
            results.Clear();
            var eval = operand.Eval();
            foreach (var val in eval.values) {
                var result = val.Floor();
                results.Add(result);
            }
            return results;
        }
    }
    
    public class Exp : UnaryArithmeticOp
    {
        public Exp(Operator operand) : base(operand) { }

        public override     string      ToString() => $"Exp({operand})";
        
        internal override EvalResult Eval() {
            results.Clear();
            var eval = operand.Eval();
            foreach (var val in eval.values) {
                var result = val.Exp();
                results.Add(result);
            }
            return results;
        }
    }
    
    public class Log : UnaryArithmeticOp
    {
        public Log(Operator operand) : base(operand) { }

        public override     string      ToString() => $"Log({operand})";
        
        internal override EvalResult Eval() {
            results.Clear();
            var eval = operand.Eval();
            foreach (var val in eval.values) {
                var result = val.Log();
                results.Add(result);
            }
            return results;
        }
    }
    
    public class Sqrt : UnaryArithmeticOp
    {
        public Sqrt(Operator operand) : base(operand) { }

        public override     string      ToString() => $"Sqrt({operand})";
        
        internal override EvalResult Eval() {
            results.Clear();
            var eval = operand.Eval();
            foreach (var val in eval.values) {
                var result = val.Sqrt();
                results.Add(result);
            }
            return results;
        }
    }
    
    
    
    
    // ------------------------------------ binary arithmetic operators ------------------------------------
    public abstract class BinaryArithmeticOp : Operator
    {
        protected           Operator            left;
        protected           Operator            right;
        internal readonly   EvalResult          results = new EvalResult(new List<SelectorValue>());
        
        protected BinaryArithmeticOp(Operator left, Operator right) {
            this.left = left;
            this.right = right;
        }
        
        internal override void Init(GraphOpContext cx) {
            cx.ValidateReuse(this); // results are reused
            left.Init(cx);
            right.Init(cx);
        }
    }
    
    public class Add : BinaryArithmeticOp
    {
        public Add(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} + {right}";
        
        internal override EvalResult Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                var result = pair.left.Add(pair.right);
                results.Add(result);
            }
            return results;
        }
    }
    
    public class Subtract : BinaryArithmeticOp
    {
        public Subtract(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} - {right}";
        
        internal override EvalResult Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                var result = pair.left.Subtract(pair.right);
                results.Add(result);
            }
            return results;
        }
    }
    
    public class Multiply : BinaryArithmeticOp
    {
        public Multiply(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} * {right}";
        
        internal override EvalResult Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                var result = pair.left.Multiply(pair.right);
                results.Add(result);
            }
            return results;
        }
    }
    
    public class Divide : BinaryArithmeticOp
    {
        public Divide(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} / {right}";
        
        internal override EvalResult Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                var result = pair.left.Divide(pair.right);
                results.Add(result);
            }
            return results;
        }
    }

}