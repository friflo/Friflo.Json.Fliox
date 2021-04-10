// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    
    public abstract class Operator
    {
        internal abstract void                  Init(GraphOpContext cx);
        internal abstract List<SelectorValue>   Eval();
        
        internal static readonly SelectorValue          True  = new SelectorValue(true); 
        internal static readonly SelectorValue          False = new SelectorValue(false);
        
        internal static readonly List<SelectorValue>    SingleTrue  = new List<SelectorValue>{ True  };
        internal static readonly List<SelectorValue>    SingleFalse = new List<SelectorValue>{ False };
        
        public static Operator FromFilter<T>(Expression<Func<T, bool>> filter) {
            return QueryConverter.OperatorFromExpression(filter);
        }
    }
    
    internal class GraphOpContext
    {
        internal readonly Dictionary<string, Field> selectors = new Dictionary<string, Field>();
        private  readonly HashSet<Operator>         operators = new HashSet<Operator>();

        internal void ValidateReuse(Operator op) {
            if (!operators.Add(op)) {
                var msg = $"Used operator instance is not applicable for reuse. Use a clone. Type: {op.GetType().Name}, instance: {op}";
                throw new InvalidOperationException(msg);
            }
        }
    }
    
    // ------------------------------------- unary operators -------------------------------------
    public class Field : Operator
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
    public abstract class Literal : Operator {
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
        
        public override     string      ToString() => value.ToString(CultureInfo.InvariantCulture);
        
        public BooleanLiteral(bool value) { this.value = value; }
        
        internal override List<SelectorValue> Eval() {
            return new List<SelectorValue> { new SelectorValue(value) };
        }
    }

    public class NullLiteral : Literal
    {
        public override     string      ToString() => "null";
        
        internal override List<SelectorValue> Eval() {
            return new List<SelectorValue> { new SelectorValue(ResultType.Null, null) };
        }
    }
}
