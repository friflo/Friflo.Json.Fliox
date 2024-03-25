// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.Hub.Utils
{
    internal struct InstanceBuffer<T> where T : class
    {
        private Stack<T> stack;
        
        internal T Get() {
            if (stack == null || stack.Count == 0)
                return null;
            return stack.Pop();
        }
        
        internal void Add(T instance) {
            if (stack == null) {
                stack = new Stack<T>();
            }
            stack.Push(instance);
        }
    }
}