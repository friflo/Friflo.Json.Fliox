// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    // -------------------------------------- comparison operators --------------------------------------
    public abstract class BinaryBoolOp : BoolOp
    {
        protected           GraphOp             left;
        protected           GraphOp             right;
        protected readonly  List<SelectorValue> results = new List<SelectorValue>();
        
        protected BinaryBoolOp(GraphOp left, GraphOp right) {
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
    public class Equals : BinaryBoolOp
    {
        public Equals(GraphOp left, GraphOp right) : base(left, right) { }

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
    
    public class NotEquals : BinaryBoolOp
    {
        public NotEquals(GraphOp left, GraphOp right) : base(left, right) { }

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
        public LessThan(GraphOp left, GraphOp right) : base(left, right) { }
        
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
        public LessThanOrEqual(GraphOp left, GraphOp right) : base(left, right) { }
        
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
        public GreaterThan(GraphOp left, GraphOp right) : base(left, right) { }
        
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
        public GreaterThanOrEqual(GraphOp left, GraphOp right) : base(left, right) { }
        
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