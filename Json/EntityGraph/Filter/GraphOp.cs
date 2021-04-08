// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    
    internal class GraphOpContext
    {
        internal readonly Dictionary<string, Field> selectors = new Dictionary<string, Field>();
    }

    public abstract class GraphOp
    {
        internal virtual void Init(GraphOpContext cx) { }

        internal virtual List<SelectorValue> Eval() {
            throw new NotImplementedException($"Eval() not implemented for: {GetType().Name}");
        }
    }
    
    // -------------------- unary operators --------------------
    
    // op: Field, String-/Number Literal, Not
    
    public class Field : GraphOp
    {
        public          string                  field;
        public          List<SelectorValue>     values = new List<SelectorValue>();

        public override string                  ToString() => field;

        internal override void Init(GraphOpContext cx) {
            cx.selectors.TryAdd(field, this);
        }

        internal override List<SelectorValue> Eval() { return values; }
    }
    
    public class StringLiteral : GraphOp
    {
        public              string      value;
        
        public override     string      ToString() => value;
        
        internal override List<SelectorValue> Eval() {
            var result = new List<SelectorValue> { new SelectorValue(value) };
            return result;
        }
    }
    
    public class NumberLiteral : GraphOp
    {
        public double       doubleValue;  // or long
    }
    
    public class BooleanLiteral : GraphOp
    {
        public bool         value;
    }

    public class NullLiteral : GraphOp
    {
    }
    
    public class NotOp : GraphOp
    {
        public BoolOp       lambda;
    }

    
    // -------------------- binary operators --------------------

    // op: Equals, NotEquals, Add, Subtract, Multiply, Divide, Remainder, Min, Max, ...
    //     All, Any, Count, Min, Max, ...

    public abstract class BoolOp : GraphOp {
    }
    
    public class Equals : BoolOp
    {
        public GraphOp      left;
        public GraphOp      right;
        
        internal override void Init(GraphOpContext cx) {
            left.Init(cx);
            right.Init(cx);
        }

        internal override List<SelectorValue> Eval() {
            var leftResult  = left.Eval();
            var rightResult = right.Eval();
            return new List<SelectorValue>{ new SelectorValue(true) }; // todo implement
        }
    }
    
    public class LessThan : BoolOp
    {
        public GraphOp      left;
        public GraphOp      right;
    }
    
    public class GreaterThan : BoolOp
    {
        public GraphOp      left;
        public GraphOp      right;
    }

    public class Any : BoolOp
    {
        public BoolOp       lambda;     // e.g.   i => i.amount < 1
        
        internal override void Init(GraphOpContext cx) {
            lambda.Init(cx);
        }
        
        internal override List<SelectorValue> Eval() {
            var trueValue = new SelectorValue(true); 
            var evalResult = lambda.Eval();
            foreach (var result in evalResult) {
                if (result.CompareTo(trueValue) == 0)
                    return new List<SelectorValue>{ new SelectorValue(true) };
            }
            return new List<SelectorValue>{ new SelectorValue(false) };
        }
    }
    
    public class All : BoolOp
    {
        public BoolOp       lambda;     // e.g.   i => i.amount < 1
    }
}
