// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Burst;  // UnityExtension.TryAdd()
using Friflo.Json.Flow.Mapper; 

// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Flow.Graph.Query
{
    [Fri.Discriminator("op")]
    [Fri.Polymorph(typeof(StringLiteral),          Discriminant = "string")]
    [Fri.Polymorph(typeof(DoubleLiteral),          Discriminant = "double")]
    [Fri.Polymorph(typeof(LongLiteral),            Discriminant = "int64")]
    [Fri.Polymorph(typeof(BoolLiteral),            Discriminant = "bool")]
    [Fri.Polymorph(typeof(NullLiteral),            Discriminant = "null")]
    public abstract class Operator
    {
        internal abstract void Init(OperatorContext cx, InitFlags flags);
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

    [Flags]
    internal enum InitFlags
    {
        ArrayField = 1
    }

    internal class OperatorContext
    {
        internal readonly List<Field>                   selectors = new List<Field>();
        private  readonly HashSet<Operator>             operators = new HashSet<Operator>();
        internal readonly Dictionary<string, Field>     parameters = new Dictionary<string, Field>();

        internal void Init() {
            selectors.Clear();
            operators.Clear();
            parameters.Clear();
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
        
        internal        string                  selector;   // == field if field starts with . otherwise appended to a lambda parameter
        internal        EvalResult              evalResult;

        public override string                  ToString() => field;
        
        public Field(string field) { this.field = field; }

        internal override void Init(OperatorContext cx, InitFlags flags) {
            bool isArrayField = (flags & InitFlags.ArrayField) != 0;
            if (field.StartsWith(".")) {
                selector = isArrayField ? field + "[=>]" : field;
            } else {
                var dotPos = field.IndexOf('.');
                if (dotPos == -1)
                    throw new InvalidOperationException("expect a dot in field name");
                var parameter = field.Substring(0, dotPos);
                var lambda = cx.parameters[parameter];
                var path = field.Substring(dotPos + 1);
                selector = lambda.field + "[=>]." + path;
            }
            cx.selectors.Add(this);
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
