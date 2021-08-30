// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform.Query.Arity;

namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // -------------------------------------- comparison operations --------------------------------------
    public abstract class BinaryBoolOp : FilterOperation
    {
        [Fri.Required]  public  Operation   left;
        [Fri.Required]  public  Operation   right;
        
        protected BinaryBoolOp() { }
        protected BinaryBoolOp(Operation left, Operation right) {
            this.left = left;
            this.right = right;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            left.Init(cx, 0);
            right.Init(cx, 0);
        }
    }
    
    // --- associative comparison operations ---
    public class Equal : BinaryBoolOp
    {
        public Equal() { }
        public Equal(Operation left, Operation right) : base(left, right) { }

        public override string      Linq => $"{left.Linq} == {right.Linq}";
        
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
        public NotEqual() { }
        public NotEqual(Operation left, Operation right) : base(left, right) { }

        public override string      Linq => $"{left.Linq} != {right.Linq}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                evalResult.Add(pair.left.CompareTo(pair.right) != 0 ? True : False);
            }
            return evalResult;
        }
    }

    // --- non-associative comparison operations -> call Order() --- 
    public class LessThan : BinaryBoolOp
    {
        public LessThan() { }
        public LessThan(Operation left, Operation right) : base(left, right) { }
        
        public override string      Linq => $"{left.Linq} < {right.Linq}";
        
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
        public LessThanOrEqual() { }
        public LessThanOrEqual(Operation left, Operation right) : base(left, right) { }
        
        public override string      Linq => $"{left.Linq} <= {right.Linq}";
        
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
        public GreaterThan() { }
        public GreaterThan(Operation left, Operation right) : base(left, right) { }
        
        public override string      Linq => $"{left.Linq} > {right.Linq}";
        
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
        public GreaterThanOrEqual() { }
        public GreaterThanOrEqual(Operation left, Operation right) : base(left, right) { }
        
        public override string      Linq => $"{left.Linq} >= {right.Linq}";
        
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