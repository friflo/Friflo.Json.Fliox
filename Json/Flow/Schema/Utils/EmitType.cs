// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema.Utils
{
    public class EmitType
    {
        public   readonly   Type            type;
        /// the mapper assigned to the type
        internal readonly   string          package;

        /// the piece of code to define the type
        internal readonly   string          content;
        /// contain type imports directly used by this type / mapper. 
        internal readonly   HashSet<Type>   imports;
        
        public   readonly   TypeSemantic    semantic;

        public   override   string          ToString() => type.Name;

        public EmitType(Type type, TypeSemantic semantic, Generator generator, StringBuilder sb, HashSet<Type> imports) {
            this.semantic   = semantic;
            this.type       = type;
            this.package    = generator.GetPackageName(type);
            this.content    = sb.ToString();
            this.imports    = imports;
        }
    }
}