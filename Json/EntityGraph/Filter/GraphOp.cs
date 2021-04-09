// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    
    public abstract class GraphOp
    {
        protected readonly  List<SelectorValue> results = new List<SelectorValue>();

        internal abstract void Init(GraphOpContext cx);

        internal virtual List<SelectorValue> Eval() {
            throw new NotImplementedException($"Eval() not implemented for: {GetType().Name}");
        }
        
        internal static readonly SelectorValue True  = new SelectorValue(true); 
        internal static readonly SelectorValue False = new SelectorValue(false);
        
        internal static readonly List<SelectorValue> SingleTrue  = new List<SelectorValue>{ True  };
        internal static readonly List<SelectorValue> SingleFalse = new List<SelectorValue>{ False };
    }
    
    internal class GraphOpContext
    {
        internal readonly Dictionary<string, Field> selectors = new Dictionary<string, Field>();
        private  readonly HashSet<GraphOp>          operators = new HashSet<GraphOp>();

        internal void ValidateOperator(GraphOp op) {
            if (!operators.Add(op))
                throw new InvalidOperationException($"Same operator cant be using multiple times in a filter: {op}");
        }
    }
    
    // ------------------------------------- unary operators -------------------------------------
    public class Field : GraphOp
    {
        public          string                  field;
        public          List<SelectorValue>     values = new List<SelectorValue>();

        public override string                  ToString() => field;
        
        public Field(string field) { this.field = field; }

        internal override void Init(GraphOpContext cx) {
            cx.selectors.TryAdd(field, this);
        }

        internal override List<SelectorValue> Eval() {
            return values;
        }
    }

    // --- primitive operators ---
    public abstract class Literal : GraphOp {
        internal override void Init(GraphOpContext cx) {
        }
    }
        
    public class StringLiteral : Literal
    {
        public              string      value;
        
        public override     string      ToString() => $"\"{value}\"";

        public StringLiteral(string value) { this.value = value; }

        internal override List<SelectorValue> Eval() {
            return new List<SelectorValue> { new SelectorValue(value) };
        }
    }
    
    public class NumberLiteral : Literal
    {
        public              double      value;  // or long

        public override     string      ToString() => value.ToString(CultureInfo.InvariantCulture);
        
        public NumberLiteral(double value) { this.value = value; }
        
        internal override List<SelectorValue> Eval() {
            return new List<SelectorValue> { new SelectorValue(value) };
        }
    }
    
    public class BooleanLiteral : Literal
    {
        public bool         value;
    }

    public class NullLiteral : Literal
    {
    }
}
