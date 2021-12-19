// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

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
        private  readonly   string          lambdaParam;
        private  readonly   List<Variable>  variables;
        
        internal Context(QueryEnv env) {
            variables   = new List<Variable>(8);
            if (env == null)
                return;
            lambdaParam = env.lambdaParam;
            if (lambdaParam != null) {
                variables.Add(new Variable(lambdaParam, VariableType.Parameter));
            }
            if (env.variables == null)
                return;
            foreach (var variable in env.variables) {
                variables.Add(new Variable(variable, VariableType.Variable));
            }
        }
        
        internal bool AddParameter(QueryNode node, out string error) {
            var param = node.ValueStr;
            if (FindVariable(param) != VariableType.NotFound) {
                error = $"parameter already used: {param} {QueryBuilder.At} {node.Pos}";
                return false;
            }
            error = null;
            variables.Add(new Variable(param, VariableType.Parameter));
            return true;
        }

        internal VariableType FindVariable(string param) {
            foreach (var variable in variables) {
                if (variable.name == param)
                    return variable.type;
            }
            return VariableType.NotFound; 
        }
        
        internal bool IsLambdaParam (string symbol) => lambdaParam == symbol;
    }
    
    internal readonly struct Variable {
        internal readonly   string          name;
        internal readonly   VariableType    type;

        public   override   string          ToString() => name;

        internal Variable(string name, VariableType type) {
            this.name   = name;
            this.type   = type;
        }
    }
    
    internal enum VariableType {
        NotFound,
        Variable,
        Parameter
    }
}