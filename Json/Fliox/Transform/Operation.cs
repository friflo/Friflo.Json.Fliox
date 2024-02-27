// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Text;
using Friflo.Json.Fliox.Transform.Query;
using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Transform.Query.Parser;

namespace Friflo.Json.Fliox.Transform
{
    // --------------------------------------- Operation ---------------------------------------
    [Discriminator("op", "operation type")]
    //
    [PolymorphType(typeof(Field),               "field")]
    //  
    [PolymorphType(typeof(StringLiteral),       "string")]
    [PolymorphType(typeof(DoubleLiteral),       "double")]
    [PolymorphType(typeof(LongLiteral),         "int64")]
    //
    [PolymorphType(typeof(NullLiteral),         "null")]
    [PolymorphType(typeof(PiLiteral),           "PI")]
    [PolymorphType(typeof(EulerLiteral),        "E")]
    [PolymorphType(typeof(TauLiteral),          "Tau")]
    //  
    [PolymorphType(typeof(Abs),                 "abs")]
    [PolymorphType(typeof(Ceiling),             "ceiling")]
    [PolymorphType(typeof(Floor),               "floor")]
    [PolymorphType(typeof(Exp),                 "exp")]
    [PolymorphType(typeof(Log),                 "log")]
    [PolymorphType(typeof(Sqrt),                "sqrt")]
    [PolymorphType(typeof(Negate),              "negate")]
    //  
    [PolymorphType(typeof(Add),                 "add")]
    [PolymorphType(typeof(Subtract),            "subtract")]
    [PolymorphType(typeof(Multiply),            "multiply")]
    [PolymorphType(typeof(Divide),              "divide")]
    [PolymorphType(typeof(Modulo),              "modulo")]
    //  
    [PolymorphType(typeof(Min),                 "min")]
    [PolymorphType(typeof(Max),                 "max")]
    [PolymorphType(typeof(Sum),                 "sum")]
    [PolymorphType(typeof(Average),             "average")]
    [PolymorphType(typeof(Count),               "count")]
    
    // --- FilterOperation
    [PolymorphType(typeof(Equal),               "equal")]
    [PolymorphType(typeof(NotEqual),            "notEqual")]
    [PolymorphType(typeof(Less),                "less")]
    [PolymorphType(typeof(LessOrEqual),         "lessOrEqual")]
    [PolymorphType(typeof(Greater),             "greater")]
    [PolymorphType(typeof(GreaterOrEqual),      "greaterOrEqual")]
    //
    [PolymorphType(typeof(And),                 "and")]
    [PolymorphType(typeof(Or),                  "or")]
    //
    [PolymorphType(typeof(TrueLiteral),         "true")]
    [PolymorphType(typeof(FalseLiteral),        "false")]
    [PolymorphType(typeof(Not),                 "not")]
    
    [PolymorphType(typeof(Lambda),              "lambda")]
    [PolymorphType(typeof(Filter),              "filter")]
    [PolymorphType(typeof(Any),                 "any")]
    [PolymorphType(typeof(All),                 "all")]
    [PolymorphType(typeof(CountWhere),          "countWhere")]
    //
    [PolymorphType(typeof(Contains),            "contains")]
    [PolymorphType(typeof(StartsWith),          "startsWith")]
    [PolymorphType(typeof(EndsWith),            "endsWith")]
    [PolymorphType(typeof(Length),              "length")]
    
    [CLSCompliant(true)]
    public abstract partial class Operation
    {
        public    abstract  string  OperationName   { get; }
        public    abstract  OpType  Type            { get; }
        internal  abstract  void    AppendLinq      (AppendCx cx);
        internal  abstract  void    Init            (OperationContext cx);
        internal  abstract  Scalar  Eval            (EvalCx cx);    // todo - use in modifier
        internal  virtual   bool    IsNumeric()     => false;
        internal  virtual   string  GetArg()        => null;
        
        public              string  Linq            { get {
            var cs = new AppendCx(new StringBuilder());
            AppendLinq(cs);
            return cs.sb.ToString();
        } }

        public    override  string  ToString() => Linq; 

        internal static readonly Scalar         True  = Scalar.True; 
        internal static readonly Scalar         False = Scalar.False;
        internal static readonly Scalar         Null  = Scalar.Null;

        public   static readonly TrueLiteral    FilterTrue  = new TrueLiteral();
        public   static readonly FalseLiteral   FilterFalse = new FalseLiteral();

        /// <summary>
        /// Parse the given <see cref="Operation"/> string and return an <see cref="Operation"/>.
        /// <returns>An <see cref="Operation"/> is successful.
        /// Otherwise it returns null and provide an descriptive <paramref name="error"/> message.</returns>
        /// </summary>
        public static Operation Parse (string operation, out string error, QueryEnv env = null) {
            return QueryBuilder.Parse(operation, out error, env);
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
    }
    
    // --------------------------------------- FilterOperation ---------------------------------------
    [Discriminator("op", "filter type")]
    // --- FilterOperation
    [PolymorphType(typeof(Equal),               "equal")]
    [PolymorphType(typeof(NotEqual),            "notEqual")]
    [PolymorphType(typeof(Less),                "less")]
    [PolymorphType(typeof(LessOrEqual),         "lessOrEqual")]
    [PolymorphType(typeof(Greater),             "greater")]
    [PolymorphType(typeof(GreaterOrEqual),      "greaterOrEqual")]
    //
    [PolymorphType(typeof(And),                 "and")]
    [PolymorphType(typeof(Or),                  "or")]
    //
    [PolymorphType(typeof(TrueLiteral),         "true")]
    [PolymorphType(typeof(FalseLiteral),        "false")]
    [PolymorphType(typeof(Not),                 "not")]
    
    [PolymorphType(typeof(Filter),              "filter")]
    [PolymorphType(typeof(Any),                 "any")]
    [PolymorphType(typeof(All),                 "all")]
    //
    [PolymorphType(typeof(Contains),            "contains")]
    [PolymorphType(typeof(StartsWith),          "startsWith")]
    [PolymorphType(typeof(EndsWith),            "endsWith")]
    
    public abstract class FilterOperation : Operation
    {
        public  bool    IsTrue => this is TrueLiteral || (this as Filter)?.body is TrueLiteral;
                     
        protected FilterOperation() { }
    }

    
    // --------------------------------------- OpType ---------------------------------------
    // ReSharper disable InconsistentNaming
    public enum OpType
    {
        FIELD           = 1,
        //  
        STRING          = 2,
        DOUBLE          = 3,
        INT64           = 4,
        //
        NULL            = 5,
        PI              = 6,
        E               = 7,
        TAU             = 8,
        //  
        ABS             = 9,
        CEILING         = 10,
        FLOOR           = 11,
        EXP             = 12,
        LOG             = 13,
        SQRT            = 14,
        NEGATE          = 15,
        //  
        ADD             = 16,
        SUBTRACT        = 17,
        MULTIPLY        = 18,
        DIVIDE          = 19,
        MODULO          = 20,
        //  
        MIN             = 21,
        MAX             = 22,
        SUM             = 23,
        AVERAGE         = 24,
        COUNT           = 25,
        
        // --- FilterOperation
        EQUAL           = 26,
        NOT_EQUAL       = 27,
        LESS            = 28,
        LESS_OR_EQUAL   = 29,
        GREATER         = 30,
        GREATER_OR_EQUAL= 31,
        //
        AND             = 32,
        OR              = 33,
        //
        TRUE            = 34,
        FALSE           = 35,
        NOT             = 36,
        //
        LAMBDA          = 37,
        FILTER          = 38,
        ANY             = 39,
        ALL             = 40,
        COUNT_WHERE     = 41,
        //
        CONTAINS        = 42,
        STARTS_WITH     = 43,
        ENDS_WITH       = 44,
        LENGTH          = 45,
    }
}
