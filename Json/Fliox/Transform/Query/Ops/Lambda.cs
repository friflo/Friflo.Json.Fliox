// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    public sealed class Lambda : Operation
    {
        [Required]  public          string          arg;
        [Required]  public          Operation       body;
        
        internal override string    GetArg()        => arg;
        public   override string    OperationName   => "(o): any";
        public   override OpType    Type            => OpType.LAMBDA;
        internal override void      AppendLinq(AppendCx cx) {
            cx.Append(arg);
            cx.Append(" => ");
            body.AppendLinq(cx);
        }

        public Lambda() { }
        public Lambda(string arg, Operation body) {
            this.arg    = arg;
            this.body   = body;
        }
        
        internal static  void InitBody (Operation body, string arg, OperationContext cx) {
            cx.initArgs.Add(arg);
            body.Init(cx);
        }
        
        internal override void Init(OperationContext cx) {
            InitBody(body, arg, cx);
        }
        
        internal override Scalar Eval(EvalCx cx) {
            return body.Eval(cx);
        }
    }
    
    public sealed class Filter : FilterOperation
    {
        [Required]  public          string          arg;
        [Required]  public          FilterOperation body;
        
        internal override string    GetArg()        => arg;
        public   override string    OperationName   => "(o): bool";
        public   override OpType    Type            => OpType.FILTER;
        internal override void      AppendLinq(AppendCx cx) {
            cx.Append(arg);
            cx.Append(" => ");
            body.AppendLinq(cx);
        }

        public Filter() { }
        public Filter(string arg, FilterOperation body) {
            this.arg    = arg;
            this.body   = body;
        }
        
        internal override void Init(OperationContext cx) {
            Ops.Lambda.InitBody(body, arg, cx);
        }
        
        internal override Scalar Eval(EvalCx cx) {
            return body.Eval(cx);
        }
    }
}