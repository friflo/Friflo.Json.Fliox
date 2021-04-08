// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
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
        
        internal static readonly SelectorValue True  = new SelectorValue(true); 
        internal static readonly SelectorValue False = new SelectorValue(false);
        
        internal static readonly List<SelectorValue> SingleTrue  = new List<SelectorValue>{ True  };
        internal static readonly List<SelectorValue> SingleFalse = new List<SelectorValue>{ False };
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
        
        public override     string      ToString() => $"\"{value}\"";
        
        internal override List<SelectorValue> Eval() {
            return new List<SelectorValue> { new SelectorValue(value) };
        }
    }
    
    public class NumberLiteral : GraphOp
    {
        public              double      value;  // or long
        
        public override     string      ToString() => value.ToString(CultureInfo.InvariantCulture);
        
        internal override List<SelectorValue> Eval() {
            return new List<SelectorValue> { new SelectorValue(value) };
        }
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
    
    public abstract class BoolOp : GraphOp { }

    public abstract class BinaryBoolOp : BoolOp
    {
        public GraphOp      left;
        public GraphOp      right;
        
        internal override void Init(GraphOpContext cx) {
            left.Init(cx);
            right.Init(cx);
        }
    }
    
    public class Equals : BinaryBoolOp
    {
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

    public class LessThan : BinaryBoolOp
    {
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
    
    public class GreaterThan : BinaryBoolOp
    {
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

    // -------------------------------------------------------------------------------------
    public abstract class UnaryBoolOp : BoolOp
    {
        public BoolOp       lambda;     // e.g.   i => i.amount < 1
        
        internal override void Init(GraphOpContext cx) {
            lambda.Init(cx);
        }
    }
    
    public class Any : UnaryBoolOp
    {
        public override     string      ToString() => $"Any({lambda})";
        
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
