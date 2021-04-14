// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using Friflo.Json.Burst; // UnityExtension.TryAdd()
using Friflo.Json.Flow.Graph.Select;

// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Flow.Graph.Query
{
    
    public abstract class Operator
    {
        internal abstract void                  Init(OperatorContext cx);
        internal abstract EvalResult            Eval(EvalCx cx);
        
        internal static readonly Scalar         True  = Scalar.True; 
        internal static readonly Scalar         False = Scalar.False;
        internal static readonly Scalar         Null  = Scalar.Null;

        internal static readonly EvalResult     SingleTrue  = new EvalResult(True);
        internal static readonly EvalResult     SingleFalse = new EvalResult(False);

        public JsonLambda Lambda() {
            return new JsonLambda(this);
        }
        
        public static Operator FromLambda<T>(Expression<Func<T, object>> lambda) {
            return QueryConverter.OperatorFromExpression(lambda);
        }
        
        public static Operator FromFilter<T>(Expression<Func<T, bool>> filter) {
            return QueryConverter.OperatorFromExpression(filter);
        }
    }

    internal readonly struct EvalCx
    {
        private readonly    int     groupIndex;

        public              int     GroupIndex => groupIndex;
        
        internal EvalCx(int groupIndex) {
            this.groupIndex = groupIndex;
        }
    }

    internal class OperatorContext
    {
        internal readonly Dictionary<string, Field> selectors = new Dictionary<string, Field>();
        private  readonly HashSet<Operator>         operators = new HashSet<Operator>();

        internal void Init() {
            selectors.Clear();
            operators.Clear();
        }

        internal void ValidateReuse(Operator op) {
            if (operators.Add(op))
                return;
            var msg = $"Used operator instance is not applicable for reuse. Use a clone. Type: {op.GetType().Name}, instance: {op}";
            throw new InvalidOperationException(msg);
        }
    }
    
    // ------------------------------------- unary operators -------------------------------------
    public class Field : Operator
    {
        public          string                  field;
        internal        EvalResult              evalResult;

        public override string                  ToString() => field;
        
        public Field(string field) { this.field = field; }

        internal override void Init(OperatorContext cx) {
            cx.selectors.TryAdd(field, this);
        }

        internal override EvalResult Eval(EvalCx cx) {
            int groupIndex = cx.GroupIndex;
            if (groupIndex == -1)
                return evalResult;
            
            var groupIndices = evalResult.groupIndices;
            int startIndex = groupIndices[groupIndex];
            int endIndex;
            if (groupIndex + 1 < groupIndices.Count) {
                endIndex = groupIndices[groupIndex + 1];
            } else {
                endIndex = evalResult.values.Count;
            }
            evalResult.SetRange(startIndex, endIndex);
            return evalResult;
        }
    }

    // --- primitive operators ---
    public abstract class Literal : Operator {
        // is set always to the same value in Eval() so it can be reused
        internal  readonly  EvalResult          evalResult = new EvalResult(new List<Scalar> {new Scalar()});
        
        internal override void Init(OperatorContext cx) { }
    }
        
    public class StringLiteral : Literal
    {
        public              string      value;
        
        public override     string      ToString() => $"'{value}'";

        public StringLiteral(string value) { this.value = value; }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(new Scalar(value));
            return evalResult;
        }
    }
    
    public class DoubleLiteral : Literal
    {
        private             double      value;

        public override     string      ToString() => value.ToString(CultureInfo.InvariantCulture);

        public DoubleLiteral(double value) { this.value = value; }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(new Scalar(value));
            return evalResult;
        }
    }
    
    public class LongLiteral : Literal
    {
        private             long      value;

        public override     string      ToString() => value.ToString();

        public LongLiteral(long value) { this.value = value; }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(new Scalar(value));
            return evalResult;
        }
    }
    
    public class BoolLiteral : Literal
    {
        public bool         value;
        
        public override     string      ToString() => value ? "true" : "false";

        public BoolLiteral(bool value) {
            this.value = value;
        }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(value ? True : False);
            return evalResult;
        }
    }

    public class NullLiteral : Literal
    {
        public override     string      ToString() => "null";


        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(Null);
            return evalResult;
        }
    }
}
