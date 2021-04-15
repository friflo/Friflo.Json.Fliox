// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Graph.Query.Arity;

namespace Friflo.Json.Flow.Graph.Query
{
    // -------------------------------------- comparison operators --------------------------------------
    public abstract class BinaryBoolOp : BoolOp
    {
        protected           Operator            left;
        protected           Operator            right;
        
        protected BinaryBoolOp(Operator left, Operator right) {
            this.left = left;
            this.right = right;
        }
        
        internal override void Init(OperatorContext cx) {
            cx.ValidateReuse(this); // results are reused
            left.Init(cx);
            right.Init(cx);
        }
    }
    
    // --- associative comparison operators ---
    public class Equal : BinaryBoolOp
    {
        public Equal(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} == {right}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                evalResult.Add(pair.left.CompareTo(pair.right) == 0 ? True : False);
            }
            return evalResult;
        }
    }
    
    public class NotEqual : BinaryBoolOp
    {
        public NotEqual(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} != {right}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                evalResult.Add(pair.left.CompareTo(pair.right) != 0 ? True : False);
            }
            return evalResult;
        }
    }

    // --- non-associative comparison operators -> call Order() --- 
    public class LessThan : BinaryBoolOp
    {
        public LessThan(Operator left, Operator right) : base(left, right) { }
        
        public override     string      ToString() => $"{left} < {right}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                evalResult.Add(pair.left.CompareTo(pair.right) < 0 ? True : False);
            }
            return evalResult;
        }
    }
    
    public class LessThanOrEqual : BinaryBoolOp
    {
        public LessThanOrEqual(Operator left, Operator right) : base(left, right) { }
        
        public override     string      ToString() => $"{left} <= {right}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                evalResult.Add(pair.left.CompareTo(pair.right) <= 0 ? True : False);
            }
            return evalResult;
        }
    }
    
    public class GreaterThan : BinaryBoolOp
    {
        public GreaterThan(Operator left, Operator right) : base(left, right) { }
        
        public override     string      ToString() => $"{left} > {right}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                evalResult.Add(pair.left.CompareTo(pair.right) > 0 ? True : False);
            }
            return evalResult;
        }
    }
    
    public class GreaterThanOrEqual : BinaryBoolOp
    {
        public GreaterThanOrEqual(Operator left, Operator right) : base(left, right) { }
        
        public override     string      ToString() => $"{left} >= {right}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                evalResult.Add(pair.left.CompareTo(pair.right) >= 0 ? True : False);
            }
            return evalResult;
        }
    }
}