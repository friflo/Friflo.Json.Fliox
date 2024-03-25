// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Transform.Query;
using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Transform
{
    // --------------------------------------- Operation.internal ---------------------------------------
    public partial class Operation
    {
        private static string GetExpressionArg(Expression exp) {
            if (exp is LambdaExpression lambda) {
                return lambda.Parameters[0].Name;
            }
            return null;
        }
        
        internal static void AppendLinqArrow(string name, Field field, string arg, Operation op, AppendCx cx) {
            var sb = cx.sb;
            field.AppendLinq(cx);
            sb.Append('.');
            sb.Append(name);
            sb.Append('(');
            sb.Append(arg);
            sb.Append(" => ");
            op.AppendLinq(cx);
            sb.Append(')');
        }
        
        internal static void AppendLinqMethod(string name, Operation symbol, Operation operand, AppendCx cx) {
            var sb = cx.sb;
            symbol.AppendLinq(cx);
            sb.Append('.');
            sb.Append(name);
            sb.Append('(');
            operand.AppendLinq(cx);
            sb.Append(')');
        }
        
        internal static void AppendLinqMethod(string name, Operation symbol, AppendCx cx) {
            var sb = cx.sb;
            symbol.AppendLinq(cx);
            sb.Append('.');
            sb.Append(name);
            sb.Append("()");
        }
        
        internal static void AppendLinqFunction(string name, Operation operand, AppendCx cx) {
            var sb = cx.sb;
            sb.Append(name);
            sb.Append('(');
            operand.AppendLinq(cx);
            sb.Append(')');
        }
        
        internal void AppendLinqBinary(AppendCx cx, string token, Operation left, Operation right) {
            var sb = cx.sb;
            AppendOperation (cx, left);
            sb.Append(' ');
            sb.Append(token);
            sb.Append(' ');
            AppendOperation (cx, right);
        }
        
        private void AppendOperation(AppendCx cx, Operation op) {
            var sb = cx.sb;
            var opPrecedence    = GetPrecedence(op);
            var precedence      = GetPrecedence(this);
            if (precedence >= opPrecedence) {
                op.AppendLinq(cx);
                return;
            }
            sb.Append('(');
            op.AppendLinq(cx);
            sb.Append(')');
        }
        
        internal void AppendLinqNAry(AppendCx cx, string token, List<FilterOperation> operands) {
            var sb = cx.sb;
            var operand     = operands[0];
            AppendOperation(cx, operand);
            for (int n = 1; n < operands.Count; n++) {
                operand                 = operands[n];
                sb.Append(' ');
                sb.Append(token);
                sb.Append(' ');
                AppendOperation(cx, operand);
            }
        }
        
        private static int GetPrecedence (Operation op) {
            switch (op) {
                // --- binary arithmetic
                case Multiply           _:  return 2;
                case Divide             _:  return 2;
                case Modulo             _:  return 2;
                case Add                _:  return 3;
                case Subtract           _:  return 3;
                // --- binary compare
                case Greater            _:  return 4;
                case GreaterOrEqual     _:  return 4;
                case Less               _:  return 4;
                case LessOrEqual        _:  return 4;
                case NotEqual           _:  return 5;
                case Equal              _:  return 5;
                // -- n-ary logical
                case And                _:  return 6;
                case Or                 _:  return 7;
             
                default:                    return 0;
            }
        }
    }
}