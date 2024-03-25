// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public sealed class QueryEnv
    {
        public readonly string                      lambdaParam;
        public readonly Dictionary<string, string>  variables;
        
        public QueryEnv (string lambdaParam, Dictionary<string, string> variables = null) {
            this.lambdaParam    = lambdaParam;
            this.variables      = variables;
        }
    }
    
    internal sealed class Context
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
            foreach (var pair in env.variables) {
                var name    = pair.Key;
                var valueOp = new Field(name); // todo should return a scalar Placeholder Operation in future (string, numeric, bool, null)
                variables.Add(new Variable(name, VariableType.Variable, valueOp));
            }
        }
        
        internal bool AddParameter(QueryNode node, out string error) {
            var param   = node.ValueStr;
            var find    = FindVariable(param); 
            if (find.type != VariableType.NotFound) {
                error = $"parameter already used: {param} {QueryBuilder.At} {node.Pos}";
                return false;
            }
            error = null;
            variables.Add(new Variable(param, VariableType.Parameter));
            return true;
        }

        internal Variable FindVariable(string param) {
            foreach (var variable in variables) {
                if (variable.name == param)
                    return variable;
            }
            return default; 
        }
        
        internal bool IsLambdaParam (string symbol) => lambdaParam == symbol;
    }
    
    internal readonly struct Variable {
        internal readonly   string          name;
        internal readonly   VariableType    type;
        internal readonly   Operation       value;

        public   override   string          ToString() => name;

        internal Variable(string name, VariableType type, Operation value = null) {
            this.name   = name;
            this.type   = type;
            this.value  = value;
        }
    }
    
    internal enum VariableType {
        NotFound,
        Variable,
        Parameter
    }
}