// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.Definition;
using Friflo.Json.Flow.Schema.JSON;

namespace Friflo.Json.Flow.Schema
{
    /// <summary>
    /// <see cref="JsonTypeOptions"/> contains the configuration used by schema and code generators using a
    /// JSON Schema as input containing the types. These types are given via <see cref="schema"/> -
    /// typically as <see cref="JsonTypeSchema"/>.
    /// <br></br>
    /// All other properties are optional (can be null) and enable customization of the generated output: a schema or code.
    /// </summary>
    public class JsonTypeOptions
    {
        public  readonly    TypeSchema              schema;
        /// <summary>the file extension of the generated files</summary>
        public              string                  fileExt;
        /// <summary>replace the namespaces / packages by the given <see cref="replacements"/></summary>
        public              ICollection<Replace>    replacements;
        /// <summary>Types which need to be created in their own specific file can be listed in <see cref="separateTypes"/>.
        /// This is typically a requirement of tools using JSON Schema <see cref="separateTypes"/></summary>
        public              ICollection<TypeDef>    separateTypes;
        /// <summary><see cref="getPath"/> allow customization of generated file names</summary>
        public              Func<TypeDef, string>   getPath;
        
        public JsonTypeOptions (TypeSchema schema) {
            this.schema     = schema ?? throw new ArgumentException("schema must not be null");
        }
    }
    
    /// <summary>
    /// <see cref="NativeTypeOptions"/> contains the configuration used by schema and code generators using the
    /// .NET type system - typically C# model classes - as input. These types are given via a <see cref="typeStore"/> and
    /// the <see cref="rootTypes"/>. The <see cref="typeStore"/> can be empty.
    /// <br></br>
    /// All other properties are optional (can be null) and enable customization of the generated output: a schema or code.
    /// </summary>
    public class NativeTypeOptions
    {
        public  readonly    TypeStore               typeStore; 
        public  readonly    ICollection<Type>       rootTypes;
        /// <summary>the file extension of the generated files</summary>
        public              string                  fileExt;
        /// <summary>replace the namespaces / packages by the given <see cref="replacements"/></summary>
        public              ICollection<Replace>    replacements;
        /// <summary>Types which need to be created in their own specific file can be listed in <see cref="separateTypes"/>.
        /// This is typically a requirement of tools using JSON Schema <see cref="separateTypes"/></summary>
        public              ICollection<Type>       separateTypes;
        /// <summary><see cref="getPath"/> allow customization of generated file names</summary>
        public              Func<TypeDef, string>   getPath;
        
        public NativeTypeOptions (TypeStore typeStore, ICollection<Type> rootTypes) {
            this.typeStore  = typeStore ?? throw new ArgumentException("typeStore must not be null");
            this.rootTypes  = rootTypes ?? throw new ArgumentException("rootTypes must not be null");
        }
    }
    
    public class Replace {
        /// <summary>The namespace which need to be replaced</summary>
        public  readonly    string @namespace;
        /// <summary>The <see cref="replacement"/> (can be "") for the given <see cref="@namespace"/>.</summary>
        public  readonly    string replacement;
        
        public Replace(string @namespace, string replacement = "") {
            this.@namespace     = @namespace;
            this.replacement    = replacement;
        }
    }
}