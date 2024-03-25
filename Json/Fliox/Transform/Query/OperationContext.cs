// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Transform.Tree;

namespace Friflo.Json.Fliox.Transform.Query
{
    public sealed class OperationContext
    {
        private             Operation                       op;
        private  readonly   HashSet<Operation>              operations  = new HashSet<Operation>();
        /// <summary>Used to ensure existence of lambda args used by <see cref="Ops.Field.name"/>'s</summary>
        internal readonly   HashSet<string>                 initArgs    = new HashSet<string>();
        private  readonly   List<ArgValue>                  args        = new List<ArgValue>();

        private             string                          error;
        
        internal            Operation                       Operation => op;

        /// <summary>
        /// Initialize <see cref="OperationContext"/> with given <see cref="op"/> and validate operation in one step.
        /// Validation is not done in a separate step to ensure validation and initialization code and result are in sync.     
        /// </summary>
        public bool Init(Operation op, out string error) {
            this.error = null;
            operations.Clear();
            this.op = op;
            op.Init(this);
            error = this.error;
            return error == null;
        }
        
        internal void Reset() {
            args.Clear();
        }
        
        internal ArgValue GetArgValue (string arg) {
            // most likely the arg is the last element => iterate backwards
            for (int n = args.Count - 1; n >= 0; n--) {
                var value = args[n];
                if (value.arg == arg) {
                    return value;
                }
            }
            throw new KeyNotFoundException($"expect lambda arg: {arg}");
        }
        
        private bool ContainsArg (string arg) {
            for (int n = args.Count - 1; n >= 0; n--) {
                if (args[n].arg == arg) {
                    return true;
                }
            }
            return false;
        }
        
        internal void AddArgValue (in ArgValue argValue) {
#if DEBUG
            if (ContainsArg(argValue.arg)) throw new ArgumentException($"arg already added. arg: {argValue.arg}");
#endif
            args.Add(argValue);
        }
        
        internal void RemoveLastArg() {
            args.RemoveAt(args.Count - 1);
        }

        internal void ValidateReuse(Operation op) {
            if (operations.Add(op))
                return;
            var msg = $"Used operation instance is not applicable for reuse. Use a clone. Type: {op.GetType().Name}, instance: {op}";
            throw new InvalidOperationException(msg);
        }
        
        internal void   Error(string message) {
            // log only first error
            if (error != null)
                return;
            error = message;
        }
    }
    
    internal sealed class ArgValue
    {
        internal readonly    string         arg;
        internal readonly    JsonAst        ast;
        internal readonly    JsonAstNode[]  nodes;
        private              int            childIndex;
        internal             int            ChildIndex  => childIndex;
        internal             bool           HasNext()   => childIndex != -1;

        public   override   string          ToString()  => arg;

        internal ArgValue(string arg, JsonAst ast, int childIndex) {
            this.arg        = arg;
            this.ast        = ast;
            this.nodes      = ast.intern.nodes;
            this.childIndex = childIndex;
        }
        
        internal void MoveNext() {
            childIndex = nodes[childIndex].next;
        }
    }
}