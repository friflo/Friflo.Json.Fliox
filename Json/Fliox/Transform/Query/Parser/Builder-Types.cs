// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public class QueryEnv
    {
        public readonly string       lambdaParam;
        public readonly List<string> variables;
        
        public QueryEnv (string lambdaParam, List<string> variables = null) {
            this.lambdaParam    = lambdaParam;
            this.variables      = variables;
        }
    }
    
    internal class Context
    {
        private  readonly   QueryEnv        env;
        private  readonly   string          lambdaParam;
        private  readonly   List<string>    parameters;
        
        internal Context(QueryEnv env) {
            this.env    = env;
            parameters      = new List<string>();
            lambdaParam = env?.lambdaParam;
            if (lambdaParam != null) {
                parameters.Add(lambdaParam);
            }
        }
        
        internal void AddParameter(string param) {
            parameters.Add(param);
        }
        
        internal bool ExistVariable(string variable) {
            if (env?.variables == null)
                return false;
            return env.variables.IndexOf(variable) != -1;
        }

        internal bool ExistParameter(string param) {
            return parameters.IndexOf(param) != -1;
        }
        
        internal bool IsLambdaParam (string symbol) => lambdaParam == symbol;
    }
    
    [Flags]
    internal enum OperandType {
        None    = 0,
        Num     = 1,
        Str     = 2,
        Bool    = 4,
        Var     = 1 | 2 | 4,
    }
    
    // ------------------------------ operand result types ------------------------------
    internal readonly struct BinaryOperands {
        internal readonly   Operation   left;
        internal readonly   Operation   right;
        
        internal BinaryOperands(Operation left, Operation right) {
            this.left   = left;
            this.right  = right;
        }
    }
    
    internal readonly struct Aggregate {
        internal readonly   Field       field;
        internal readonly   string      arg; 
        internal readonly   Operation   operand;
        
        internal Aggregate(Field field, string arg, Operation operand) {
            this.field      = field;
            this.arg        = arg;
            this.operand    = operand;
        }
    }
    
    internal readonly struct Quantify {
        internal readonly   Field           field;
        internal readonly   string          arg; 
        internal readonly   FilterOperation filter;
        
        internal Quantify(Field field, string arg, FilterOperation filter) {
            this.field      = field;
            this.arg        = arg;
            this.filter     = filter;
        }
    }
}