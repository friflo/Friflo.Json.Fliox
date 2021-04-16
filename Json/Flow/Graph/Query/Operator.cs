// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Burst; // UnityExtension.TryAdd()

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
        
        public static BoolOp FromFilter<T>(Expression<Func<T, bool>> filter) {
            return (BoolOp)QueryConverter.OperatorFromExpression(filter);
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
}
