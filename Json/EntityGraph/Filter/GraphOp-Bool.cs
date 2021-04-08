// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    public abstract class BoolOp : GraphOp { }

    // ---------------------------------------- BinaryBoolOp ----------------------------------------
    public abstract class BinaryBoolOp : BoolOp
    {
        public GraphOp      left;
        public GraphOp      right;

        protected BinaryBoolOp(GraphOp left, GraphOp right) {
            this.left = left;
            this.right = right;
        }
        
        internal override void Init(GraphOpContext cx) {
            left.Init(cx);
            right.Init(cx);
        }
    }
    
    public class Equals : BinaryBoolOp
    {
        public Equals(GraphOp left, GraphOp right) : base(left, right) { }

        public override     string      ToString() => $"{left} == {right}";
        
        internal override List<SelectorValue> Eval() {
            var result = new BinaryResult(left.Eval(), right.Eval());
            foreach (var value in result.values) {
                if (result.value.CompareTo(value) == 0)
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }
    
    public class NotEquals : BinaryBoolOp
    {
        public NotEquals(GraphOp left, GraphOp right) : base(left, right) { }

        public override     string      ToString() => $"{left} != {right}";
        
        internal override List<SelectorValue> Eval() {
            var result = new BinaryResult(left.Eval(), right.Eval());
            foreach (var value in result.values) {
                if (result.value.CompareTo(value) != 0)
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }

    public class LessThan : BinaryBoolOp
    {
        public LessThan(GraphOp left, GraphOp right) : base(left, right) { }
        
        public override     string      ToString() => $"{left} < {right}";
        
        internal override List<SelectorValue> Eval() {
            var result = new BinaryResult(left.Eval(), right.Eval());
            foreach (var value in result.values) {
                if (result.Order(result.value.CompareTo(value) < 0))
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }
    
    public class LessThanOrEqual : BinaryBoolOp
    {
        public LessThanOrEqual(GraphOp left, GraphOp right) : base(left, right) { }
        
        public override     string      ToString() => $"{left} <= {right}";
        
        internal override List<SelectorValue> Eval() {
            var result = new BinaryResult(left.Eval(), right.Eval());
            foreach (var value in result.values) {
                if (result.Order(result.value.CompareTo(value) <= 0))
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }
    
    public class GreaterThan : BinaryBoolOp
    {
        public GreaterThan(GraphOp left, GraphOp right) : base(left, right) { }
        
        public override     string      ToString() => $"{left} > {right}";
        
        internal override List<SelectorValue> Eval() {
            var result = new BinaryResult(left.Eval(), right.Eval());
            foreach (var value in result.values) {
                if (result.Order(result.value.CompareTo(value) > 0))
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }
    
    public class GreaterThanOrEqual : BinaryBoolOp
    {
        public GreaterThanOrEqual(GraphOp left, GraphOp right) : base(left, right) { }
        
        public override     string      ToString() => $"{left} >= {right}";
        
        internal override List<SelectorValue> Eval() {
            var result = new BinaryResult(left.Eval(), right.Eval());
            foreach (var value in result.values) {
                if (result.Order(result.value.CompareTo(value) >= 0))
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }
    
    internal readonly struct  BinaryResult
    {
        internal readonly SelectorValue         value;
        internal readonly List<SelectorValue>   values;
        internal readonly bool                  swap;

        internal BinaryResult(List<SelectorValue> left, List<SelectorValue> right) {
            if (left.Count == 1) {
                value   = left[0];
                values  = right;
                swap    = false;
                return;
            }
            if (right.Count == 1) {
                value   = right[0];
                values  = left;
                swap    = true;
                return;
            }
            throw new InvalidOperationException("Expect at least an operation result with one element");
        }

        internal bool Order(bool condition) {
            return swap ? !condition : condition;
        }
    }

    // ---------------------------------------- UnaryBoolOp ----------------------------------------
    public abstract class UnaryBoolOp : BoolOp
    {
        public BoolOp       lambda;     // e.g.   i => i.amount < 1

        public UnaryBoolOp(BoolOp lambda) { this.lambda = lambda; }
        
        internal override void Init(GraphOpContext cx) {
            lambda.Init(cx);
        }
    }
    
    public class Any : UnaryBoolOp
    {
        public override     string      ToString() => $"Any({lambda})";
        
        public Any(BoolOp lambda) : base(lambda) { }
        
        internal override List<SelectorValue> Eval() {
            var evalResult = lambda.Eval();
            foreach (var result in evalResult) {
                if (result.CompareTo(True) == 0)
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }
    
    public class All : UnaryBoolOp
    {
        public override     string      ToString() => $"All({lambda})";
        
        public All(BoolOp lambda) : base(lambda) { }
        
        internal override List<SelectorValue> Eval() {
            var evalResult = lambda.Eval();
            foreach (var result in evalResult) {
                if (result.CompareTo(True) != 0)
                    return SingleFalse;
            }
            return SingleTrue;
        }
    }
}