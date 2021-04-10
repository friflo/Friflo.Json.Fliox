// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.EntityGraph.Filter.Arity;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
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
        
        internal override void Init(GraphOpContext cx) {
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
        
        internal override List<SelectorValue> Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                results.Add(pair.left.CompareTo(pair.right) == 0 ? True : False);
            }
            return results;
        }
    }
    
    public class NotEqual : BinaryBoolOp
    {
        public NotEqual(Operator left, Operator right) : base(left, right) { }

        public override     string      ToString() => $"{left} != {right}";
        
        internal override List<SelectorValue> Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                results.Add(pair.left.CompareTo(pair.right) != 0 ? True : False);
            }
            return results;
        }
    }

    // --- non-associative comparison operators -> call Order() --- 
    public class LessThan : BinaryBoolOp
    {
        public LessThan(Operator left, Operator right) : base(left, right) { }
        
        public override     string      ToString() => $"{left} < {right}";
        
        internal override List<SelectorValue> Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                results.Add(pair.left.CompareTo(pair.right) < 0 ? True : False);
            }
            return results;
        }
    }
    
    public class LessThanOrEqual : BinaryBoolOp
    {
        public LessThanOrEqual(Operator left, Operator right) : base(left, right) { }
        
        public override     string      ToString() => $"{left} <= {right}";
        
        internal override List<SelectorValue> Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                results.Add(pair.left.CompareTo(pair.right) <= 0 ? True : False);
            }
            return results;
        }
    }
    
    public class GreaterThan : BinaryBoolOp
    {
        public GreaterThan(Operator left, Operator right) : base(left, right) { }
        
        public override     string      ToString() => $"{left} > {right}";
        
        internal override List<SelectorValue> Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                results.Add(pair.left.CompareTo(pair.right) > 0 ? True : False);
            }
            return results;
        }
    }
    
    public class GreaterThanOrEqual : BinaryBoolOp
    {
        public GreaterThanOrEqual(Operator left, Operator right) : base(left, right) { }
        
        public override     string      ToString() => $"{left} >= {right}";
        
        internal override List<SelectorValue> Eval() {
            results.Clear();
            var eval = new BinaryResult(left.Eval(), right.Eval());
            foreach (var pair in eval) {
                results.Add(pair.left.CompareTo(pair.right) >= 0 ? True : False);
            }
            return results;
        }
    }
}