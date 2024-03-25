// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Transform.Query
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
            if (arrayIndex == -1) {
                item            = new ArgValue(arg, ast, -1);
            } else {
                var array       = argValue.nodes[arrayIndex];
                item            = new ArgValue(arg, ast, array.child);
            }
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
}