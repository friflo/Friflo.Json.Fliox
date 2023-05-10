// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Hub.Host.Utils
{
    public class FilterArgs
    {
        private readonly   Dictionary<string, string>   args;
        public  readonly   Filter                       filter;
        
        public FilterArgs(Filter filter) {
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
            args        = new Dictionary<string, string>();
        }
        
        public string GetArg(Field field) {
            var arg = field.arg;
            if (args.TryGetValue(arg, out string result)) {
                return result;
            }
            throw new InvalidOperationException($"argument not found in {field.name}. arg: {arg}");
        }
        
        public ArgScope AddArg(string arg, string alias = null) {
            alias ??= arg;
            args.Add(arg, alias);
            return new ArgScope(this, arg);
        }
        
        public void RemoveArg(string arg) {
            args.Remove(arg);
        }
    }
    
    public readonly struct ArgScope : IDisposable
    {
        private readonly FilterArgs args;
        private readonly string     argument;
        
        public ArgScope(FilterArgs args, string argument) {
            this.args       = args;
            this.argument   = argument;
        }
            
        public void Dispose() {
            args.RemoveArg(argument);
        }
    }
}