// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Transform.Tree;

// ReSharper disable ReplaceSliceWithRangeIndexer
// ReSharper disable ReplaceSubstringWithRangeIndexer
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // ------------------------------------- unary operations -------------------------------------
    public sealed class Field : Operation
    {
        [Required]  public      string      name;
        [Ignore]    public      string      arg;
        [Ignore]    internal    Utf8Bytes[] pathItems;

        public   override string    OperationName           => "name";
        internal override void      AppendLinq(AppendCx cx) { cx.Append(name); }

        public Field() { }
        public Field(string name) { this.name = name; }

        internal override void Init(OperationContext cx)
        {
            var dotPos = name.IndexOf('.');
            if (dotPos == 0) {
                cx.Error($"invalid field name '{name}'");
                return;
            }
            if (dotPos == -1) {
                pathItems   = Array.Empty<Utf8Bytes>();
                arg         = name;
            } else {
                var fields  = name.AsSpan().Slice(dotPos + 1);
                pathItems   = JsonAst.GetPathItems(fields);
                arg         = name.Substring(0, dotPos);
            }
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
}