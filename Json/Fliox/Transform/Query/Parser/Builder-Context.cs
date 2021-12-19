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
        
        internal bool AddParameter(QueryNode node, out string error) {
            var param = node.ValueStr;
            if (ExistParameter(param)) {
                error = $"parameter already used: {param} {QueryBuilder.At} {node.Pos}";
                return false;
            }
            error = null;
            parameters.Add(param);
            return true;
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

}