// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using Friflo.Json.Fliox.Transform.Query;
using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Transform.Query.Parser;

namespace Friflo.Json.Fliox.Transform
{
    [Discriminator("op", Description = "operation type")]
    //
    [PolymorphType(typeof(Field),               Discriminant = "field")]
    //  
    [PolymorphType(typeof(StringLiteral),       Discriminant = "string")]
    [PolymorphType(typeof(DoubleLiteral),       Discriminant = "double")]
    [PolymorphType(typeof(LongLiteral),         Discriminant = "int64")]
    //
    [PolymorphType(typeof(NullLiteral),         Discriminant = "null")]
    [PolymorphType(typeof(PiLiteral),           Discriminant = "PI")]
    [PolymorphType(typeof(EulerLiteral),        Discriminant = "E")]
    [PolymorphType(typeof(TauLiteral),          Discriminant = "Tau")]
    //  
    [PolymorphType(typeof(Abs),                 Discriminant = "abs")]
    [PolymorphType(typeof(Ceiling),             Discriminant = "ceiling")]
    [PolymorphType(typeof(Floor),               Discriminant = "floor")]
    [PolymorphType(typeof(Exp),                 Discriminant = "exp")]
    [PolymorphType(typeof(Log),                 Discriminant = "log")]
    [PolymorphType(typeof(Sqrt),                Discriminant = "sqrt")]
    [PolymorphType(typeof(Negate),              Discriminant = "negate")]
    //  
    [PolymorphType(typeof(Add),                 Discriminant = "add")]
    [PolymorphType(typeof(Subtract),            Discriminant = "subtract")]
    [PolymorphType(typeof(Multiply),            Discriminant = "multiply")]
    [PolymorphType(typeof(Divide),              Discriminant = "divide")]
    [PolymorphType(typeof(Modulo),              Discriminant = "modulo")]
    //  
    [PolymorphType(typeof(Min),                 Discriminant = "min")]
    [PolymorphType(typeof(Max),                 Discriminant = "max")]
    [PolymorphType(typeof(Sum),                 Discriminant = "sum")]
    [PolymorphType(typeof(Average),             Discriminant = "average")]
    [PolymorphType(typeof(Count),               Discriminant = "count")]
    
    // --- FilterOperation
    [PolymorphType(typeof(Equal),               Discriminant = "equal")]
    [PolymorphType(typeof(NotEqual),            Discriminant = "notEqual")]
    [PolymorphType(typeof(Less),                Discriminant = "less")]
    [PolymorphType(typeof(LessOrEqual),         Discriminant = "lessOrEqual")]
    [PolymorphType(typeof(Greater),             Discriminant = "greater")]
    [PolymorphType(typeof(GreaterOrEqual),      Discriminant = "greaterOrEqual")]
    //
    [PolymorphType(typeof(And),                 Discriminant = "and")]
    [PolymorphType(typeof(Or),                  Discriminant = "or")]
    //
    [PolymorphType(typeof(TrueLiteral),         Discriminant = "true")]
    [PolymorphType(typeof(FalseLiteral),        Discriminant = "false")]
    [PolymorphType(typeof(Not),                 Discriminant = "not")]
    
    [PolymorphType(typeof(Lambda),              Discriminant = "lambda")]
    [PolymorphType(typeof(Filter),              Discriminant = "filter")]
    [PolymorphType(typeof(Any),                 Discriminant = "any")]
    [PolymorphType(typeof(All),                 Discriminant = "all")]
    [PolymorphType(typeof(CountWhere),          Discriminant = "countWhere")]
    //
    [PolymorphType(typeof(Contains),            Discriminant = "contains")]
    [PolymorphType(typeof(StartsWith),          Discriminant = "startsWith")]
    [PolymorphType(typeof(EndsWith),            Discriminant = "endsWith")]
    
    // ----------------------------- Operation --------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class Operation
    {
        public    abstract  string      OperationName   { get; }
        public    abstract  void        AppendLinq      (AppendCx cx);
        internal  abstract  void        Init            (OperationContext cx, InitFlags flags);
        internal  abstract  EvalResult  Eval            (EvalCx cx);
        internal  virtual   bool        IsNumeric       => false;
        public              string      Linq            { get {
            var cs = new AppendCx(new StringBuilder());
            AppendLinq(cs);
            return cs.sb.ToString();
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
        
        /// <summary>
        /// Parse the given <see cref="Operation"/> string and return an <see cref="Operation"/>.
        /// <returns>An <see cref="Operation"/> is successful.
        /// Otherwise it returns null and provide an descriptive <paramref name="error"/> message.</returns>
        /// </summary>
        public static Operation Parse (string operation, out string error, QueryEnv env = null) {
            return QueryBuilder.Parse(operation, out error, env);
        }
        
        private static string GetExpressionArg(Expression exp) {
            if (exp is LambdaExpression lambda) {
                return lambda.Parameters[0].Name;
            }
            return null;
        }
        
        public static Operation FromLambda<T>(Expression<Func<T, object>> lambda, QueryPath queryPath = null) {
            var op = QueryConverter.OperationFromExpression(lambda, queryPath);
            var arg = GetExpressionArg(lambda);
            return new Lambda(arg, op);

        }
        
        public static FilterOperation FromFilter<T>(Expression<Func<T, bool>> filter, QueryPath queryPath = null) {
            var op = (FilterOperation)QueryConverter.OperationFromExpression(filter, queryPath);
            var arg = GetExpressionArg(filter);
            return new Filter(arg, op);
        }
        
        public static string PathFromLambda<T>(Expression<Func<T, object>> path, QueryPath queryPath = null) {
            var op = QueryConverter.OperationFromExpression(path, queryPath);
            var field = op as Field;
            if (field == null) {
                throw new ArgumentException($"path must not use operations. Only use of fields and properties is valid. path: {op}");
            }
            return field.name;
        }
        
        protected static void AppendLinqArrow(string name, Field field, string arg, Operation op, AppendCx cx) {
            var sb = cx.sb;
            field.AppendLinq(cx);
            sb.Append('.');
            sb.Append(name);
            sb.Append('(');
            sb.Append(arg);
            sb.Append(" => ");
            op.AppendLinq(cx);
            sb.Append(')');
        }
        
        protected static void AppendLinqMethod(string name, Operation symbol, Operation operand, AppendCx cx) {
            var sb = cx.sb;
            symbol.AppendLinq(cx);
            sb.Append('.');
            sb.Append(name);
            sb.Append('(');
            operand.AppendLinq(cx);
            sb.Append(')');
        }
        
        protected static void AppendLinqFunction(string name, Operation operand, AppendCx cx) {
            var sb = cx.sb;
            sb.Append(name);
            sb.Append('(');
            operand.AppendLinq(cx);
            sb.Append(')');
        }
        
        protected void AppendLinqBinary(AppendCx cx, string token, Operation left, Operation right) {
            var sb = cx.sb;
            AppendOperation (cx, left);
            sb.Append(' ');
            sb.Append(token);
            sb.Append(' ');
            AppendOperation (cx, right);
        }
        
        private void AppendOperation(AppendCx cx, Operation op) {
            var sb = cx.sb;
            var opPrecedence    = GetPrecedence(op);
            var precedence      = GetPrecedence(this);
            if (precedence >= opPrecedence) {
                op.AppendLinq(cx);
                return;
            }
            sb.Append('(');
            op.AppendLinq(cx);
            sb.Append(')');
        }
        
        protected void AppendLinqNAry(AppendCx cx, string token, List<FilterOperation> operands) {
            var sb = cx.sb;
            var operand     = operands[0];
            AppendOperation(cx, operand);
            for (int n = 1; n < operands.Count; n++) {
                operand                 = operands[n];
                sb.Append(' ');
                sb.Append(token);
                sb.Append(' ');
                AppendOperation(cx, operand);
            }
        }
        
        private static int GetPrecedence (Operation op) {
            switch (op) {
                // --- binary arithmetic
                case Multiply           _:  return 2;
                case Divide             _:  return 2;
                case Modulo             _:  return 2;
                case Add                _:  return 3;
                case Subtract           _:  return 3;
                // --- binary compare
                case Greater            _:  return 4;
                case GreaterOrEqual     _:  return 4;
                case Less               _:  return 4;
                case LessOrEqual        _:  return 4;
                case NotEqual           _:  return 5;
                case Equal              _:  return 5;
                // -- n-ary logical
                case And                _:  return 6;
                case Or                 _:  return 7;
             
                default:                    return 0;
            }
        }
    }
    
    [Discriminator("op", Description = "filter type")]
    // --- FilterOperation
    [PolymorphType(typeof(Equal),               Discriminant = "equal")]
    [PolymorphType(typeof(NotEqual),            Discriminant = "notEqual")]
    [PolymorphType(typeof(Less),                Discriminant = "less")]
    [PolymorphType(typeof(LessOrEqual),         Discriminant = "lessOrEqual")]
    [PolymorphType(typeof(Greater),             Discriminant = "greater")]
    [PolymorphType(typeof(GreaterOrEqual),      Discriminant = "greaterOrEqual")]
    //
    [PolymorphType(typeof(And),                 Discriminant = "and")]
    [PolymorphType(typeof(Or),                  Discriminant = "or")]
    //
    [PolymorphType(typeof(TrueLiteral),         Discriminant = "true")]
    [PolymorphType(typeof(FalseLiteral),        Discriminant = "false")]
    [PolymorphType(typeof(Not),                 Discriminant = "not")]
    
    [PolymorphType(typeof(Filter),              Discriminant = "filter")]
    [PolymorphType(typeof(Any),                 Discriminant = "any")]
    [PolymorphType(typeof(All),                 Discriminant = "all")]
    //
    [PolymorphType(typeof(Contains),            Discriminant = "contains")]
    [PolymorphType(typeof(StartsWith),          Discriminant = "startsWith")]
    [PolymorphType(typeof(EndsWith),            Discriminant = "endsWith")]
    
    // ----------------------------- FilterOperation --------------------------
    public abstract class FilterOperation : Operation
    {
        [Ignore]    public   readonly  QueryFormat query;
        [Ignore]    internal readonly  EvalResult  evalResult = new EvalResult(new List<Scalar>());
                    public             bool        IsTrue => this is TrueLiteral || (this as Filter)?.body is TrueLiteral;
                     
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
            return QueryCosmos.ToCosmos(collection, filter);
        } }

        internal QueryFormat (FilterOperation filter) {
            this.filter = filter;
        }
    }
}
