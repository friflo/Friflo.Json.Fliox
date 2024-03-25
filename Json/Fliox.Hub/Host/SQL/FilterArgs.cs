// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public sealed class FilterArgs
    {
        private readonly    Dictionary<string, string>      args;
        private readonly    Dictionary<string, ArrayField>  arrayFields;
        public  readonly    FilterOperation                 filter;
        
        public override     string                          ToString() => filter.Linq;
        
        public FilterArgs(FilterOperation filter) {
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
            args        = new Dictionary<string, string>();
            arrayFields = new Dictionary<string, ArrayField>();
        }
        
        // --- arg's
        public string GetArg(Field field) {
            var arg = field.arg;
            if (args.TryGetValue(arg, out string result)) {
                return result;
            }
            return arg;
        }
        
        public ArgScope AddArg(string arg, string alias = null) {
            alias ??= arg;
            args.Add(arg, alias);
            return new ArgScope(this, arg);
        }
        
        public void RemoveArg(string arg) {
            args.Remove(arg);
        }
        
        // --- arrayField's
        public ArrayField GetArrayField(Field field) {
            arrayFields.TryGetValue(field.arg, out var result);
            return result;
        }
        
        public FieldArrayScope AddArrayField(string field, string array) {
            var arrayField = new ArrayField(field, array);
            arrayFields.Add(field, arrayField);
            return new FieldArrayScope(this, field);
        }
        
        public void RemoveArrayField(string field) {
            arrayFields.Remove(field);
        }
    }
    
    public readonly struct ArgScope : IDisposable
    {
        private readonly    FilterArgs  args;
        private readonly    string      argument;
        
        public override     string      ToString() => argument;
        
        public ArgScope(FilterArgs args, string argument) {
            this.args       = args;
            this.argument   = argument;
        }
            
        public void Dispose() {
            args.RemoveArg(argument);
        }
    }
    
    public sealed class ArrayField
    {
        public readonly     string  field;
        public readonly     string  array;

        public override     string  ToString() => $"{field} in {array}";

        public ArrayField(string field, string array) {
            this.field  = field ?? throw new ArgumentNullException(nameof(field));
            this.array  = array ?? throw new ArgumentNullException(nameof(array));
        }
    }
    
    public readonly struct FieldArrayScope : IDisposable
    {
        private readonly    FilterArgs  args;
        private readonly    string      field;
        
        public override     string      ToString() => field;
        
        public FieldArrayScope(FilterArgs args, string field) {
            this.args   = args;
            this.field  = field;
        }
            
        public void Dispose() {
            args.RemoveArrayField(field);
        }
    }
}