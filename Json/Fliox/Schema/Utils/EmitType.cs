// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Utils
{
    public sealed class EmitType
    {
        public   readonly   TypeDef                 type;
        /// the mapper assigned to the type
        internal readonly   string                  path;

        /// the piece of code to define the type
        internal readonly   string                  content;
        /// contain type imports directly used by this type / mapper. 
        internal readonly   HashSet<TypeDef>        imports;
        
        internal readonly   ICollection<TypeDef>    typeDependencies;
        internal readonly   ICollection<EmitType>   emitDependencies = new List<EmitType>();

        public   override   string                  ToString() => type.Name;

        public EmitType(TypeDef type,
            StringBuilder       sb,
            HashSet<TypeDef>    imports         = null,
            List<TypeDef>       dependencies    = null)
        {
            this.type               = type;
            this.path               = type.Path;
            this.content            = sb.ToString();
            this.imports            = imports       ?? new HashSet<TypeDef>();
            this.typeDependencies   = dependencies  ?? new List<TypeDef>();
        }
    }
}