// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema.Definition
{
    /// <summary>
    /// Contains the all required data to generate code for a type.
    /// Note: This file does and must not have any dependency to <see cref="System.Type"/>.
    /// </summary>
    public abstract class TypeDef {
        public              string              Name         { get; set; }
        /// <summary>
        /// Namespace of a type. Depending on the generated language / schema is has the following meaning:
        /// <list type="bullet">
        ///   <item>The <see cref="System.Type.Namespace"/> of a <see cref="System.Type"/>in C#</item>
        ///   <item>The module (file) in Typescript (Javascript)</item>
        ///   <item>The schema file in JSON Schema</item>
        ///   <item>The package folder in Java</item>
        /// </list> 
        /// </summary>
        public              string              Namespace    { get; set; }
        /// <summary>The path of the file a type is generated.
        /// <br></br>
        /// In Typescript or JSON Schema a file can contain multiple types.
        /// These types have the same file <see cref="Path"/> and the same <see cref="Namespace"/>.
        /// <br></br>
        /// In Java each type require its own file. The <see cref="Namespace"/> is the Java package name which can be
        /// used by multiple types. But each type has its individual file <see cref="Path"/>.
        /// </summary>
        public              string              Path         { get; internal set; }
        
        /// The class this type extends. In other words its base or parent class.  
        public  abstract    TypeDef             BaseType     { get; }
        
        /// If <see cref="IsComplex"/> is true it has <see cref="Fields"/>
        public  abstract    bool                IsComplex    { get; }
        public  abstract    List<FieldDef>      Fields       { get; }
        
        /// <see cref="UnionType"/> is not null, if the type is as discriminated union.
        public  abstract    UnionType           UnionType    { get; }
        /// <see cref="Discriminant"/> is not null, if the type is an element of a <see cref="UnionType"/>
        public  abstract    string              Discriminant { get; }
        
        /// If <see cref="IsEnum"/> is true it has <see cref="EnumValues"/>
        public  abstract    bool                IsEnum       { get; }
        public  abstract    ICollection<string> EnumValues   { get; }
        /// currently not used
        public  abstract    TypeSemantic        TypeSemantic { get; }

        public  abstract    bool                IsDerivedField(FieldDef fieldDef);
    }
    
    public class FieldDef {
        public  readonly    string          name;
        public  readonly    bool            required;
        public  readonly    TypeDef         type;
        /// if <see cref="isArray"/> is true <see cref="type"/> contains the element type.
        public  readonly    bool            isArray;
        /// if <see cref="isDictionary"/> is true <see cref="type"/> contains the value type.
        public  readonly    bool            isDictionary;

        public  override    string          ToString() => name;
        
        public FieldDef(string name, bool required, TypeDef type, bool isArray, bool isDictionary) {
            this.name           = name;
            this.required       = required;
            this.type           = type;
            this.isArray        = isArray;
            this.isDictionary   = isDictionary;
        }
        
    }

    public class UnionType {
        public              string          discriminator;
        public              List<TypeDef>   types;
        
        public   override   string          ToString() => discriminator;
    }
}