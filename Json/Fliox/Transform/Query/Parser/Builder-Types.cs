// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public class QueryEnv
    {
        public readonly string       arg;
        public readonly List<string> variables;
        
        public QueryEnv (string arg, List<string> variables = null) {
            this.arg        = arg;
            this.variables  = variables;
        }
    }
    
    internal class Context
    {
        private  readonly   QueryEnv        env;
        private             string          arg;
        internal readonly   List<string>    locals;
        
        internal Context(QueryEnv env) {
            this.env    = env;
            arg         = env != null ? env.arg + "." : null;
            locals      = new List<string>();
        }
        
        internal void SetArg(string arg) {
            if (this.arg != null)
                throw new InvalidOperationException("arg already set");
            this.arg = arg + ".";
        }
        
        internal bool ExistVariable(string variable) {
            if (locals.IndexOf(variable) != -1)
                return true;
            if (env == null)
                return false;
            if (env.arg == variable)
                return true;
            if (env.variables == null)
                return false;
            return env.variables.IndexOf(variable) != -1;
        }
        
        internal string GetFieldName (string name) {
            if (arg != null && name.StartsWith(arg)) {
                return name.Substring(arg.Length - 1);
            }
            return name;
        }
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