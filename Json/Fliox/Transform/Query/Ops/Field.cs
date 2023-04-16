// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Transform.Tree;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable ConvertToAutoProperty
// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    internal readonly struct EvalCx
    {
        internal readonly   OperationContext    opContext;
        
        internal EvalCx(OperationContext opContext) {
            this.opContext  = opContext;
        }
        
        internal ArgScope AddArrayArg(string arg, Field field, out ArgValue item) {
            var argValue    = opContext.GetArgValue(field.arg);
            var ast         = argValue.ast;
            int arrayIndex  = ast.GetPathNodeIndex(argValue.ChildIndex, field.pathItems);
            var array       = argValue.nodes[arrayIndex];
            item            = new ArgValue(arg, ast, array.child);
            opContext.AddArgValue(item);
            return new ArgScope(opContext);
        }
        
        internal int CountArray(Field field) {
            var argValue    = opContext.GetArgValue(field.arg);
            int arrayIndex  = argValue.ast.GetPathNodeIndex(argValue.ChildIndex, field.pathItems);
            if (arrayIndex == -1) {
                return 0;
            }
            var nodes       = argValue.nodes;
            var array       = nodes[arrayIndex];
            int count       = 0;
            int childIndex  = array.child;
            while (childIndex != -1) {
                count++;
                childIndex = nodes[childIndex].next;
            }
            return count;
        }
    }
    
    internal readonly struct ArgScope : IDisposable
    {
        private readonly OperationContext    opContext;
        
        internal ArgScope(OperationContext opContext) {
            this.opContext  = opContext;
        }
        
        public void Dispose() {
            opContext.RemoveLastArg();
        }
    }

    
    // ------------------------------------- unary operations -------------------------------------
    public sealed class Field : Operation
    {
        [Required]  public      string      name;
        [Ignore]    internal    string      arg;
        [Ignore]    internal    Utf8Bytes[] pathItems;

        public   override string    OperationName => "name";
        public   override void      AppendLinq(AppendCx cx) {
            cx.Append(name);
        }

        public Field() { }
        public Field(string name) { this.name = name; }

        internal override void Init(OperationContext cx)
        {
            // bool isArrayField = (flags & InitFlags.ArrayField) != 0;

            var dotPos = name.IndexOf('.');
            if (dotPos <= 0) {
                cx.Error($"invalid field name '{name}'");
                return;
            }
            var fields  = name.AsSpan().Slice(dotPos + 1);
            pathItems   = JsonAst.GetPathItems(fields);
            arg         = name.Substring(0, dotPos);
            if (!cx.initArgs.Contains(arg)) {
                cx.Error($"symbol '{arg}' not found");
            }
        }

        internal override Scalar Eval(EvalCx cx) {
            var argValue = cx.opContext.GetArgValue(arg);
            argValue.ast.GetPathValue(argValue.ChildIndex, pathItems, out var value);
            return value;
        }
    }
    
    internal class ArgValue
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

    public sealed class OperationContext
    {
        private             Operation                       op;
        private  readonly   HashSet<Operation>              operations  = new HashSet<Operation>();
        /// <summary>Used to ensure existence of lambda args used by <see cref="Field.name"/>'s</summary>
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
    
    public struct AppendCx {
        public          string          lambdaArg;
        public readonly StringBuilder   sb;
        
        // ReSharper disable once UnusedParameter.Local
        public AppendCx (StringBuilder sb) {
            this.sb     = sb;
            lambdaArg   = "";
        }

        public override string ToString() => sb.ToString();

        public void Append(string str) {
            sb.Append(str);
        }
    }
}