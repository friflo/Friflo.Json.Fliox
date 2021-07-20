// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Flow.Transform.Query.Ops
{
    internal readonly struct EvalCx
    {
        private readonly    int     groupIndex;

        public              int     GroupIndex => groupIndex;
        
        internal EvalCx(int groupIndex) {
            this.groupIndex = groupIndex;
        }
    }

    
    // ------------------------------------- unary operations -------------------------------------
    public class Field : Operation
    {
        [Fri.Property(Required = true)]
        public          string                  name;
        
        [Fri.Ignore]
        internal        string                  selector;   // == field if field starts with . otherwise appended to a lambda parameter
        [Fri.Ignore]
        internal        EvalResult              evalResult;

        public override string                  Linq => name;

        public Field() { }
        public Field(string name) { this.name = name; }

        internal override void Init(OperationContext cx, InitFlags flags) {
            bool isArrayField = (flags & InitFlags.ArrayField) != 0;
            if (name.StartsWith(".")) {
                selector = isArrayField ? name + "[=>]" : name;
            } else {
                var dotPos = name.IndexOf('.');
                if (dotPos == -1)
                    throw new InvalidOperationException("expect a dot in field name");
                var arg = name.Substring(0, dotPos);
                var lambda = cx.lambdaArgs[arg];
                var path = name.Substring(dotPos + 1);
                selector = lambda.name + "[=>]." + path;
            }
            cx.selectors.Add(this);
        }

        internal override EvalResult Eval(EvalCx cx) {
            int groupIndex = cx.GroupIndex;
            if (groupIndex == -1)
                return evalResult;
            
            var groupIndices = evalResult.groupIndices;
            if (groupIndices.Count == 0) {
                evalResult.SetRange(0, 0);
                return evalResult;
            }
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
    
    [Flags]
    internal enum InitFlags
    {
        ArrayField = 1
    }

    internal class OperationContext
    {
        internal readonly List<Field>                   selectors = new List<Field>();
        private  readonly HashSet<Operation>            operations = new HashSet<Operation>();
        internal readonly Dictionary<string, Field>     lambdaArgs = new Dictionary<string, Field>();

        internal void Init() {
            selectors.Clear();
            operations.Clear();
            lambdaArgs.Clear();
        }

        internal void ValidateReuse(Operation op) {
            if (operations.Add(op))
                return;
            var msg = $"Used operation instance is not applicable for reuse. Use a clone. Type: {op.GetType().Name}, instance: {op}";
            throw new InvalidOperationException(msg);
        }
    }
}