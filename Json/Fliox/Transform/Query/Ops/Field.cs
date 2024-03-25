// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Transform.Tree;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming
// ReSharper disable ReplaceSliceWithRangeIndexer
// ReSharper disable ReplaceSubstringWithRangeIndexer
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // ------------------------------------- unary operations -------------------------------------
    public sealed class Field : Operation
    {
        [Required]  public      string      name { get => nameIntern; set => SetArg(value); }
        [Ignore]    public      string      arg  { get; private set; }
        [Ignore]    internal    Utf8Bytes[] pathItems;
        
        // --- private
        [Ignore]    private     string      nameIntern;

        public   override string    OperationName           => "name";
        public   override OpType    Type                    => OpType.FIELD;
        internal override void      AppendLinq(AppendCx cx) { cx.Append(name); }

        public Field() { }
        public Field(string name) { this.name = name; }
        
        private void SetArg(string value) {
            nameIntern = value;
            var dotPos = value.IndexOf('.');
            if (dotPos == -1) {
                arg   = value;
            } else {
                arg   = value.Substring(0, dotPos);
            }
        }

        internal override void Init(OperationContext cx)
        {
            var dotPos = name.IndexOf('.');
            if (dotPos == 0) {
                cx.Error($"invalid field name '{name}'");
                return;
            }
            if (dotPos == -1) {
                pathItems   = Array.Empty<Utf8Bytes>();
            } else {
                var fields  = name.AsSpan().Slice(dotPos + 1);
                pathItems   = JsonAst.GetPathItems(fields);
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