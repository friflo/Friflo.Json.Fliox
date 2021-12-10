// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform.Query;
using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Transform.Query.Parser;

namespace Friflo.Json.Fliox.Transform
{
    [Fri.Discriminator("op")]
    //
    [Fri.Polymorph(typeof(Field),               Discriminant = "field")]
    //  
    [Fri.Polymorph(typeof(StringLiteral),       Discriminant = "string")]
    [Fri.Polymorph(typeof(DoubleLiteral),       Discriminant = "double")]
    [Fri.Polymorph(typeof(LongLiteral),         Discriminant = "int64")]
    [Fri.Polymorph(typeof(NullLiteral),         Discriminant = "null")]
    //  
    [Fri.Polymorph(typeof(Abs),                 Discriminant = "abs")]
    [Fri.Polymorph(typeof(Ceiling),             Discriminant = "ceiling")]
    [Fri.Polymorph(typeof(Floor),               Discriminant = "floor")]
    [Fri.Polymorph(typeof(Exp),                 Discriminant = "exp")]
    [Fri.Polymorph(typeof(Log),                 Discriminant = "log")]
    [Fri.Polymorph(typeof(Sqrt),                Discriminant = "sqrt")]
    [Fri.Polymorph(typeof(Negate),              Discriminant = "negate")]
    //  
    [Fri.Polymorph(typeof(Add),                 Discriminant = "add")]
    [Fri.Polymorph(typeof(Subtract),            Discriminant = "subtract")]
    [Fri.Polymorph(typeof(Multiply),            Discriminant = "multiply")]
    [Fri.Polymorph(typeof(Divide),              Discriminant = "divide")]
    //  
    [Fri.Polymorph(typeof(Min),                 Discriminant = "min")]
    [Fri.Polymorph(typeof(Max),                 Discriminant = "max")]
    [Fri.Polymorph(typeof(Sum),                 Discriminant = "sum")]
    [Fri.Polymorph(typeof(Average),             Discriminant = "average")]
    [Fri.Polymorph(typeof(Count),               Discriminant = "count")]
    
    // --- FilterOperation  
    [Fri.Polymorph(typeof(Equal),               Discriminant = "equal")]
    [Fri.Polymorph(typeof(NotEqual),            Discriminant = "notEqual")]
    [Fri.Polymorph(typeof(LessThan),            Discriminant = "lessThan")]
    [Fri.Polymorph(typeof(LessThanOrEqual),     Discriminant = "lessThanOrEqual")]
    [Fri.Polymorph(typeof(GreaterThan),         Discriminant = "greaterThan")]
    [Fri.Polymorph(typeof(GreaterThanOrEqual),  Discriminant = "greaterThanOrEqual")]
    //
    [Fri.Polymorph(typeof(And),                 Discriminant = "and")]
    [Fri.Polymorph(typeof(Or),                  Discriminant = "or")]
    //
    [Fri.Polymorph(typeof(TrueLiteral),         Discriminant = "true")]
    [Fri.Polymorph(typeof(FalseLiteral),        Discriminant = "false")]
    [Fri.Polymorph(typeof(Not),                 Discriminant = "not")]
    [Fri.Polymorph(typeof(Any),                 Discriminant = "any")]
    [Fri.Polymorph(typeof(All),                 Discriminant = "all")]
    [Fri.Polymorph(typeof(CountWhere),          Discriminant = "countWhere")]
    //
    [Fri.Polymorph(typeof(Contains),            Discriminant = "contains")]
    [Fri.Polymorph(typeof(StartsWith),          Discriminant = "startsWith")]
    [Fri.Polymorph(typeof(EndsWith),            Discriminant = "endsWith")]
    
    // ----------------------------- Operation --------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class Operation
    {
        public    abstract  void        AppendLinq (StringBuilder sb);
        internal  abstract  void        Init (OperationContext cx, InitFlags flags);
        internal  abstract  EvalResult  Eval (EvalCx cx);
        public              string      Linq { get {
            var sb = new StringBuilder();
            AppendLinq(sb);
            return sb.ToString();
        } }

        public    override  string      ToString() => Linq; 

        internal static readonly Scalar         True  = Scalar.True; 
        internal static readonly Scalar         False = Scalar.False;
        internal static readonly Scalar         Null  = Scalar.Null;

        internal static readonly EvalResult     SingleTrue  = new EvalResult(True);
        internal static readonly EvalResult     SingleFalse = new EvalResult(False);
        
        public   static readonly TrueLiteral    FilterTrue  = new TrueLiteral();
        public   static readonly FalseLiteral   FilterFalse = new FalseLiteral();

       
        public JsonLambda Lambda() {
            return new JsonLambda(this);
        }
        
        public static Operation Parse (string operation, out string error) {
            return QueryParser.Parse(operation, out error);
        }
        
        public static Operation FromLambda<T>(Expression<Func<T, object>> lambda, QueryPath queryPath = null) {
            return QueryConverter.OperationFromExpression(lambda, queryPath);
        }
        
        public static FilterOperation FromFilter<T>(Expression<Func<T, bool>> filter, QueryPath queryPath = null) {
            return (FilterOperation)QueryConverter.OperationFromExpression(filter, queryPath);
        }
        
        public static string PathFromLambda<T>(Expression<Func<T, object>> path, QueryPath queryPath = null) {
            var op = QueryConverter.OperationFromExpression(path, queryPath);
            var field = op as Field;
            if (field == null) {
                throw new ArgumentException($"path must not use operations. Only use of fields and properties is valid. path: {op}");
            }
            return field.name;
        }
        
        protected static void AppendLinqArrow(string name, Field field, string arg, Operation op, StringBuilder sb) {
            field.AppendLinq(sb);
            sb.Append(".");
            sb.Append(name);
            sb.Append("(");
            sb.Append(arg);
            sb.Append(" => ");
            op.AppendLinq(sb);
            sb.Append(")");
        }
        
        protected static void AppendLinqMethod(string name, Operation symbol, Operation operand, StringBuilder sb) {
            symbol.AppendLinq(sb);
            sb.Append(".");
            sb.Append(name);
            sb.Append("(");
            operand.AppendLinq(sb);
            sb.Append(")");
        }
        
        protected static void AppendLinqFunction(string name, Operation operand, StringBuilder sb) {
            sb.Append(name);
            sb.Append("(");
            operand.AppendLinq(sb);
            sb.Append(")");
        }
        
        protected void AppendLinqBinary(StringBuilder sb, string token, Operation left, Operation right) {
            AppendOperation (sb, left);
            sb.Append(' ');
            sb.Append(token);
            sb.Append(' ');
            AppendOperation (sb, right);
        }
        
        private void AppendOperation(StringBuilder sb, Operation op) {
            var opPrecedence    = GetPrecedence(op);
            var precedence      = GetPrecedence(this);
            if (precedence > opPrecedence) {
                op.AppendLinq(sb);
                return;
            }
            sb.Append('(');
            op.AppendLinq(sb);
            sb.Append(')');
        }
        
        protected void AppendLinqNAry(StringBuilder sb, string token, List<FilterOperation> operands) {
            var operand     = operands[0];
            AppendOperation(sb, operand);
            for (int n = 1; n < operands.Count; n++) {
                operand                 = operands[n];
                sb.Append(' ');
                sb.Append(token);
                sb.Append(' ');
                AppendOperation(sb, operand);
            }
        }
        
        private static int GetPrecedence (Operation op) {
            switch (op) {
                // --- binary arithmetic
                case Multiply           _:  return 2;
                case Divide             _:  return 2;
                case Add                _:  return 3;
                case Subtract           _:  return 3;
                // --- binary compare
                case GreaterThan        _:  return 4;
                case GreaterThanOrEqual _:  return 4;
                case LessThan           _:  return 4;
                case LessThanOrEqual    _:  return 4;
                case NotEqual           _:  return 5;
                case Equal              _:  return 5;
                // -- n-ary logical
                case And                _:  return 6;
                case Or                 _:  return 7;
             
                default:                    return 0;
            }
        }
    }
    
    [Fri.Discriminator("op")]
    // --- FilterOperation  
    [Fri.Polymorph(typeof(Equal),               Discriminant = "equal")]
    [Fri.Polymorph(typeof(NotEqual),            Discriminant = "notEqual")]
    [Fri.Polymorph(typeof(LessThan),            Discriminant = "lessThan")]
    [Fri.Polymorph(typeof(LessThanOrEqual),     Discriminant = "lessThanOrEqual")]
    [Fri.Polymorph(typeof(GreaterThan),         Discriminant = "greaterThan")]
    [Fri.Polymorph(typeof(GreaterThanOrEqual),  Discriminant = "greaterThanOrEqual")]
    //
    [Fri.Polymorph(typeof(And),                 Discriminant = "and")]
    [Fri.Polymorph(typeof(Or),                  Discriminant = "or")]
    //
    [Fri.Polymorph(typeof(TrueLiteral),         Discriminant = "true")]
    [Fri.Polymorph(typeof(FalseLiteral),        Discriminant = "false")]
    [Fri.Polymorph(typeof(Not),                 Discriminant = "not")]
    [Fri.Polymorph(typeof(Any),                 Discriminant = "any")]
    [Fri.Polymorph(typeof(All),                 Discriminant = "all")]
    //
    [Fri.Polymorph(typeof(Contains),            Discriminant = "contains")]
    [Fri.Polymorph(typeof(StartsWith),          Discriminant = "startsWith")]
    [Fri.Polymorph(typeof(EndsWith),            Discriminant = "endsWith")]
    
    // ----------------------------- FilterOperation --------------------------
    public abstract class FilterOperation : Operation
    {
        [Fri.Ignore] public   readonly  QueryFormat     query;
        [Fri.Ignore] internal readonly  EvalResult      evalResult = new EvalResult(new List<Scalar>());
                     
        protected FilterOperation() {
            query    = new QueryFormat(this);
        }

        public JsonFilter Filter() {
            return new JsonFilter(this);
        }        
    }
    
    public readonly struct QueryFormat {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly    FilterOperation     filter;
        
        public              string              Cosmos { get {
            var collection = "c";
            return $"SELECT * FROM {collection} {QueryCosmos.ToCosmos(collection, filter)}";
        } }

        internal QueryFormat (FilterOperation filter) {
            this.filter = filter;
        }
    }
}
