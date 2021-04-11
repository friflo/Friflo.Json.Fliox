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
        internal abstract EvalResult            Eval();
        
        internal static readonly SelectorValue          True  = new SelectorValue(true); 
        internal static readonly SelectorValue          False = new SelectorValue(false);
        
        internal static readonly EvalResult             SingleTrue  = new EvalResult(True);
        internal static readonly EvalResult             SingleFalse = new EvalResult(False);
        
        public static Operator FromFilter<T>(Expression<Func<T, bool>> filter) {
            return QueryConverter.OperatorFromExpression(filter);
        }
        
        public static Operator FromLambda<T>(Expression<Func<T, object>> lambda) {
            return QueryConverter.OperatorFromExpression(lambda);
        }
    }

    internal struct EvalResult
    {
        internal List<SelectorValue> values;

        internal EvalResult (SelectorValue singleValue) {
            values = new List<SelectorValue> { singleValue };
        }
        
        internal EvalResult (List<SelectorValue> values) {
            this.values = values;
        }

        internal int Count => values.Count;

        internal void Clear() {
            values.Clear();
        }
        
        internal void Add(SelectorValue value) {
            values.Add(value);
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
        internal        EvalResult              results = new EvalResult(new List<SelectorValue>());

        public override string                  ToString() => field;
        
        public Field(string field) { this.field = field; }

        internal override void Init(GraphOpContext cx) {
            cx.selectors.TryAdd(field, this);
        }

        internal override EvalResult Eval() {
            return results;
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
        
        public override     string      ToString() => $"'{value}'";

        public StringLiteral(string value) { this.value = value; }

        internal override EvalResult Eval() {
            return new EvalResult(new SelectorValue(value));
        }
    }
    
    public class DoubleLiteral : Literal
    {
        private             double      value;

        public override     string      ToString() => value.ToString(CultureInfo.InvariantCulture);

        public DoubleLiteral(double value) { this.value = value;  }
        
        internal override EvalResult Eval() {
            return new EvalResult(new SelectorValue(value));
        }
    }
    
    public class LongLiteral : Literal
    {
        private             long      value;

        public override     string      ToString() => value.ToString();

        public LongLiteral(long value) { this.value = value;  }
        
        internal override EvalResult Eval() {
            return new EvalResult(new SelectorValue(value));
        }
    }
    
    public class BoolLiteral : Literal
    {
        public bool         value;
        
        public override     string      ToString() => value ? "true" : "false";
        
        public BoolLiteral(bool value) { this.value = value; }
        
        internal override EvalResult Eval() {
            return new EvalResult(new SelectorValue(value));
        }
    }

    public class NullLiteral : Literal
    {
        public override     string      ToString() => "null";
        
        internal override EvalResult Eval() {
            return new EvalResult(new SelectorValue(ResultType.Null, null));
        }
    }
}
