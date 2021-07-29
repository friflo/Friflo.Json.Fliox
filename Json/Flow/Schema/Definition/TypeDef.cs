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
        public              string              Name         { get; }
        /// <summary>
        /// Namespace of a type. Depending on the generated language / schema is has the following meaning:
        /// <list type="bullet">
        ///   <item>The <see cref="System.Type.Namespace"/> of a <see cref="System.Type"/>in C#</item>
        ///   <item>The module (file) in Typescript (Javascript)</item>
        ///   <item>The schema file in JSON Schema</item>
        ///   <item>The package folder in Java</item>
        /// </list> 
        /// </summary>
        public              string              Namespace    { get; }
        /// <summary>The path of the file containing the generated type. It is set by <see cref="Generator"/>.
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
        /// <summary><see cref="IsStruct"/> can be true only, if <see cref="IsComplex"/> is true</summary>
        public  abstract    bool                IsStruct     { get; }
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
        
        protected TypeDef (string name, string @namespace) {
            Name        = name;
            Namespace   = @namespace;
        }
    }
    
    public class FieldDef {
        public  readonly    string          name;
        public  readonly    bool            required;
        public  readonly    TypeDef         type;
        /// if <see cref="isArray"/> is true <see cref="type"/> contains the element type.
        public  readonly    bool            isArray;
        /// if <see cref="isDictionary"/> is true <see cref="type"/> contains the value type.
        public  readonly    bool            isDictionary;
        public  readonly    TypeDef         ownerType;

        public  override    string          ToString() => name;
        
        public FieldDef(string name, bool required, TypeDef type, bool isArray, bool isDictionary, TypeDef ownerType) {
            this.name           = name;
            this.required       = required;
            this.type           = type;
            this.isArray        = isArray;
            this.isDictionary   = isDictionary;
            this.ownerType      = ownerType;
        }
        
        public bool IsDerivedField { get {
            var parent = ownerType.BaseType;
            while (parent != null) {
                if (parent.Fields.Find(f => f.name == name) != null)
                    return true;
                parent = parent.BaseType;
            }
            return false;
        }}
    }

    public class UnionType {
        public  readonly    string          discriminator;
        public  readonly    List<TypeDef>   types;
        
        public   override   string          ToString() => discriminator;
        
        public UnionType(string discriminator, List<TypeDef> types) {
            this.discriminator  = discriminator;
            this.types          = types;
        }
    }
}