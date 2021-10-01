// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.Schema.Definition
{
    /// <summary>
    /// Contains the all required data to generate code for a type.
    /// Note: This file does and must not have any dependency to <see cref="System.Type"/>.
    /// </summary>
    public abstract class TypeDef {
        public              string              Name            { get; }
        /// <summary>
        /// Namespace of a type. Depending on the generated language / schema is has the following meaning:
        /// <list type="bullet">
        ///   <item>The <see cref="System.Type.Namespace"/> of a <see cref="System.Type"/>in C#</item>
        ///   <item>The module (file) in Typescript (Javascript)</item>
        ///   <item>The schema file in JSON Schema</item>
        ///   <item>The package folder in Java</item>
        ///   <item>The package declaration in Kotlin</item>
        /// </list> 
        /// </summary>
        public              string              Namespace       { get; }
        /// <summary>The path of the file containing the generated type. It is set by <see cref="Generator"/>.
        /// <br></br>
        /// In Typescript or JSON Schema a file can contain multiple types.
        /// These types have the same file <see cref="Path"/> and the same <see cref="Namespace"/>.
        /// <br></br>
        /// In Java each type require its own file. The <see cref="Namespace"/> is the Java package name which can be
        /// used by multiple types. But each type has its individual file <see cref="Path"/>.
        /// </summary>
        public              string              Path            { get; internal set; }
        
        /// The class this type extends. In other words its base or parent class.  
        public  abstract    TypeDef             BaseType        { get; }
        
        /// If <see cref="IsClass"/> is true it has <see cref="Fields"/>
        public  abstract    bool                IsClass         { get; }
        /// <summary><see cref="IsStruct"/> can be true only, if <see cref="IsClass"/> is true</summary>
        public  abstract    bool                IsStruct        { get; }
        public  abstract    List<FieldDef>      Fields          { get; }
        
        /// <summary><see cref="UnionType"/> is not null, if the type is as discriminated union.</summary>
        public  abstract    UnionType           UnionType       { get; }
        public  abstract    bool                IsAbstract      { get; }
        /// <summary><see cref="Discriminant"/> is not null if the type is an element of a <see cref="UnionType"/>
        /// Either both <see cref="Discriminant"/> and <see cref="Discriminator"/> are not null or both are null</summary>
        public  abstract    string              Discriminant    { get; }
        public  abstract    string              Discriminator   { get; }
        
        /// If <see cref="IsEnum"/> is true it has <see cref="EnumValues"/>
        public  abstract    bool                IsEnum          { get; }
        public  abstract    ICollection<string> EnumValues      { get; }
        
        protected TypeDef (string name, string @namespace) {
            Name        = name;
            Namespace   = @namespace;
        }
    }
    
    // could by a readonly struct - but may be used by reference in future 
    public sealed class FieldDef {
        public  readonly    string          name;
        public  readonly    bool            required;
        public  readonly    bool            isKey;
        public  readonly    bool            isAutoIncrement;
        public  readonly    TypeDef         type;
        /// if <see cref="isArray"/> is true <see cref="type"/> contains the element type.
        public  readonly    bool            isArray;
        /// if <see cref="isDictionary"/> is true <see cref="type"/> contains the value type.
        public  readonly    bool            isDictionary;
        /// See <see cref="JSON.JsonTypeSchema.GetItemsFieldType"/>
        public  readonly    bool            isNullableElement;  
        public  readonly    TypeDef         ownerType;
        public              bool            IsDerivedField { get; private set; }

        public  override    string          ToString() => name;
        
        public FieldDef(string name, bool required, bool isKey, bool isAutoIncrement, TypeDef type, bool isArray, bool isDictionary, bool isNullableElement, TypeDef ownerType) {
            this.name               = name;
            this.required           = required;
            this.isKey              = isKey;
            this.isAutoIncrement    = isAutoIncrement;
            this.type               = type;
            this.isArray            = isArray;
            this.isDictionary       = isDictionary;
            this.isNullableElement  = isNullableElement;
            this.ownerType          = ownerType;
        }
        
        internal void MarkDerivedField() {
            var parent = ownerType.BaseType;
            while (parent != null) {
                foreach (var field in parent.Fields) {
                    if (field.name == name) {
                        IsDerivedField = true;
                        return;
                    }
                }
                parent = parent.BaseType;
            }
        }
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