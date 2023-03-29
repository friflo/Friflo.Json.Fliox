// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

// ReSharper disable ConvertToAutoProperty
// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
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
    public sealed class Field : Operation, ISelector
    {
        [Required]  public      string      name;
        [Ignore]    internal    string      selector;   // == field if field starts with . otherwise appended to a lambda parameter
        [Ignore]    internal    EvalResult  evalResult;

        public   override string    OperationName => "name";
        public   override void      AppendLinq(AppendCx cx) {
            cx.Append(name);
        }

        public Field() { }
        public Field(string name) { this.name = name; }

        internal override void Init(OperationContext cx, InitFlags flags)
        {
            bool isArrayField = (flags & InitFlags.ArrayField) != 0;

            var dotPos = name.IndexOf('.');
            if (dotPos <= 0) {
                cx.Error($"invalid field name '{name}'");
                return;
            }
            var arg     = name.Substring(0, dotPos);
            if (!cx.variables.TryGetValue(arg, out var lambda)) {
                cx.Error($"symbol '{arg}' not found");
                return;
            }
            var path    = name.Substring(dotPos);
            selector    = lambda.GetName(isArrayField, path);

            cx.selectors.Add(this);
        }
        
        public string GetName(bool isArrayField, string path) {
            return selector + path; // .e.g  selector = ".items[*]"
        }

        internal override EvalResult Eval(EvalCx cx) {
         /* if (evalResult.values.Count == 0) {
                evalResult.Add(Null);
                return evalResult;
            } */
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
    
    internal interface ISelector {
        string GetName(bool isArrayField, string path);
    }
    
    internal sealed class LambdaArg : ISelector
    {
        internal static readonly LambdaArg Instance = new LambdaArg();
        
        private LambdaArg () { }

        public string GetName(bool isArrayField, string path) {
            return isArrayField ? path + "[*]" : path;
        }
    }

    public sealed class OperationContext
    {
        private             Operation                       op;
        internal readonly   List<Field>                     selectors = new List<Field>();
        private  readonly   HashSet<Operation>              operations = new HashSet<Operation>();
        internal readonly   Dictionary<string, ISelector>   variables  = new Dictionary<string, ISelector>();
        private             string                          error;
        
        internal            Operation                       Operation => op;

        /// <summary>
        /// Initialize <see cref="OperationContext"/> with given <see cref="op"/> and validate operation in one step.
        /// Validation is not done in a separate step to ensure validation and initialization code and result are in sync.     
        /// </summary>
        public bool Init(Operation op, out string error) {
            this.error = null;
            selectors.Clear();
            operations.Clear();
            variables.Clear();
            this.op = op;
            op.Init(this, 0);
            error = this.error;
            return error == null;
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