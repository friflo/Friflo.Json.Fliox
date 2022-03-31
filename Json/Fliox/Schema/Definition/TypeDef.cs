// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.OAS;

namespace Friflo.Json.Fliox.Schema.Definition
{
    /// <summary>
    /// Contains the all required data to generate code for a type.<br/>
    /// Note: This file does and must not have any dependency to <see cref="System.Type"/>.<br/>
    /// Note: Instances of <see cref="TypeDef"/> including its fields are immutable.
    /// </summary>
    public abstract class TypeDef {
        public              string                      Name            { get; }
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
        public              string                      Namespace       { get; }
        /// <summary>The path of the file containing the generated type. It is set by <see cref="Language.Generator"/>.
        /// <br></br>
        /// In Typescript or JSON Schema a file can contain multiple types.
        /// These types have the same file <see cref="Path"/> and the same <see cref="Namespace"/>.
        /// <br></br>
        /// In Java each type require its own file. The <see cref="Namespace"/> is the Java package name which can be
        /// used by multiple types. But each type has its individual file <see cref="Path"/>.
        /// </summary>
        public              string                      Path            { get; internal set; }
        
        /// The class this type extends. In other words its base or parent class.  
        public   abstract   TypeDef                     BaseType        { get; }
        
        /// If <see cref="IsClass"/> is true it has <see cref="Fields"/>
        public   abstract   bool                        IsClass         { get; }
        /// <summary><see cref="IsStruct"/> can be true only, if <see cref="IsClass"/> is true</summary>
        public   abstract   bool                        IsStruct        { get; }
        public              string                      KeyField        => keyField;
        public   abstract   IReadOnlyList<FieldDef>     Fields          { get; }
        
        public   abstract   IReadOnlyList<MessageDef>   Messages        { get; }
        public   abstract   IReadOnlyList<MessageDef>   Commands        { get; }
        public              bool                        IsSchema        => Commands != null; 
        
        /// <summary><see cref="UnionType"/> is not null, if the type is as discriminated union.</summary>
        public   abstract   UnionType                   UnionType       { get; }
        public   abstract   bool                        IsAbstract      { get; }
        /// <summary><see cref="Discriminant"/> is not null if the type is an element of a <see cref="UnionType"/>
        /// Either both <see cref="Discriminant"/> and <see cref="Discriminator"/> are not null or both are null</summary>
        public   abstract   string                      Discriminant    { get; }
        public   abstract   string                      Discriminator   { get; }
        public   abstract   string                      DiscriminatorDoc{ get; }
        
        /// If <see cref="IsEnum"/> is true it has <see cref="EnumValues"/>
        public   abstract   bool                        IsEnum          { get; }
        public   abstract   IReadOnlyList<EnumValue>    EnumValues      { get; }
        internal readonly   string                      fullName;
        internal            string                      keyField;
        public   readonly   string                      doc;
        public              OpenAPI                     openAPI;

        
        protected TypeDef (string name, string @namespace, string doc) {
            fullName    = @namespace + "#" + name;
            Name        = name;
            Namespace   = @namespace;
            this.doc    = doc;
        }
        
        internal FieldDef FindField(string name) {
            foreach (var field in Fields) {
                if (field.name == name)
                    return field;
            }
            return null;
        }
    }
    
    // could by a readonly struct - but may be used by reference in future
    /// <summary>
    /// The type definition of field (also named property) in a <see cref="TypeDef"/>. E.g. a scalar type like boolean,
    /// int, float, double, DateTime, Guid, BigInteger or string or a complex type like an array, a map (= Dictionary) or a class.
    /// Fields also have a modifier to specify if a field is required or optional.
    /// <br/>
    /// As <see cref="FieldDef"/>'s are also used within in a <see cref="TypeSchema"/> to define a database schema
    /// a field can be selected to be the primary key of a table / container in a database by <see cref="isKey"/>.
    /// To simplify code generation the primary key is exposed by <see cref="TypeDef.KeyField"/>.
    /// </summary>
    public sealed class FieldDef {
        public   readonly   string          name;
        public   readonly   bool            required;
        public   readonly   bool            isKey;
        public   readonly   bool            isAutoIncrement;
        public   readonly   TypeDef         type;
        /// if <see cref="isArray"/> is true <see cref="type"/> contains the element type.
        public   readonly   bool            isArray;
        /// if <see cref="isDictionary"/> is true <see cref="type"/> contains the value type.
        public   readonly   bool            isDictionary;
        /// See <see cref="JSON.JsonTypeSchema.GetItemsFieldType"/>
        public   readonly   bool            isNullableElement;  
        private  readonly   TypeDef         ownerType;
        public              bool            IsDerivedField { get; private set; }
        public   readonly   string          relation;
        public              TypeDef         RelationType => relationType;
        internal            TypeDef         relationType;
        public   readonly   string          doc;
        

        public  override    string          ToString() => name;
        
        public FieldDef(
            string      name,
            bool        required,
            bool        isKey,
            bool        isAutoIncrement,
            TypeDef     type,
            bool        isArray,
            bool        isDictionary,
            bool        isNullableElement,
            TypeDef     ownerType,
            string      relation,
            string      doc)
        {
            this.name               = name;
            this.required           = required;
            this.isKey              = isKey;
            this.isAutoIncrement    = isAutoIncrement;
            this.type               = type;
            this.isArray            = isArray;
            this.isDictionary       = isDictionary;
            this.isNullableElement  = isNullableElement;
            this.ownerType          = ownerType;
            this.relation           = relation;
            this.doc                = doc;
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
    
    public readonly struct EnumValue
    {
        public   readonly   string      name;
        public   readonly   string      doc;

        public   override   string      ToString() => name;

        private EnumValue (string name, string doc) {
            this.name   = name;
            this.doc    = doc;
        }
        
        internal static List<EnumValue> CreateEnumValues(ICollection<string> enumNames, IReadOnlyDictionary<string,string> enumDocs) {
            if (enumNames == null)
                return null;
            var enumValues  = new List<EnumValue>(enumNames.Count);
            foreach (var name in enumNames) {
                string doc = null; 
                enumDocs?.TryGetValue(name, out doc);
                enumValues.Add(new EnumValue(name, doc));
            }
            return enumValues;
        }
    }
    
    /// <summary>
    /// <see cref="MessageDef"/> is used to specify the interface of a command (= RPC) within a service.
    /// The structure of a command consists of its <see cref="name"/> its command <see cref="param"/> type and its
    /// command <see cref="result"/> type. The command <see cref="param"/> type specify the parameters and
    /// when a command is executed it returns an object of the given <see cref="result"/> type.
    /// </summary>
    public sealed class MessageDef {
        public   readonly   string      name;
        /// <summary>null: missing param    <br/>not null: message/command param: Type</summary>
        public   readonly   FieldDef    param;
        /// <summary>null: is message       <br/>not null: is command</summary>
        public   readonly   FieldDef    result;
        public   readonly   string      doc;

        public   override   string      ToString() => name;
        
        public MessageDef(string name, FieldDef param, FieldDef result, string doc) {
            this.name       = name;
            this.param      = param;
            this.result     = result;
            this.doc        = doc;
        }
    }
    

    public sealed class UnionType {
        public   readonly   string                      discriminator;
        public   readonly   string                      doc;
        public   readonly   IReadOnlyList<UnionItem>    types;
        
        public   override   string                      ToString() => discriminator;
        
        public UnionType(string discriminator, string doc, List<UnionItem> types) {
            this.discriminator  = discriminator;
            this.doc            = doc;
            this.types          = types;
        }
    }
    
    public readonly struct UnionItem
    {
        public   readonly   TypeDef     typeDef;
        public   readonly   string      discriminant;
        
        public UnionItem (TypeDef typeDef, string discriminant) {
            this.typeDef        = typeDef;
            this.discriminant   = discriminant ?? throw new ArgumentNullException(nameof(discriminant));
        }
    }
}