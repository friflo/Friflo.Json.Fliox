// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map;

// Note: This file does and must not have any dependency to <see cref="System.Type"/>.
namespace Friflo.Json.Fliox.Schema.Definition
{
    /// <summary>
    /// Declare the shape of a specific type within a <see cref="TypeSchema"/>.<br/>
    /// <br/>
    /// If <see cref="IsSchema"/> is true the type is the root type of a database schema.<br/>
    /// In this each field in <see cref="Fields"/> represents a database container / table.  
    /// </summary>
    /// <remarks>
    /// Note: Instances of <see cref="TypeDef"/> including its fields are immutable.
    /// </remarks>
    public abstract class TypeDef {
        public              string                      Name            { get; }
        public   readonly   Utf8String                  nameUtf8;
        public   abstract   string                      AssemblyName    { get; }
        public              StandardTypeId              TypeId          { get; }

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
        public              bool                        IsClass         => Fields != null;
        /// <summary><see cref="IsStruct"/> can be true only, if <see cref="IsClass"/> is true</summary>
        public   abstract   bool                        IsStruct        { get; }
        /// <summary>Defines the field used as the primary key for a database container / table</summary>
        public              FieldDef                    KeyField        => keyField;
        /// <summary>List of class fields if <see cref="TypeDef"/> is a class. Otherwise null</summary>
        public   abstract   IReadOnlyList<FieldDef>     Fields          { get; }
        
        /// <summary>
        /// List of messages exposed by a <c>DatabaseService</c>.<br/>
        /// In contrast to <see cref="Commands"/> database service <see cref="Messages"/> have no return type.
        /// </summary>
        public   abstract   IReadOnlyList<MessageDef>   Messages        { get; }
        /// <summary>List of commands exposed by a <c>DatabaseService</c></summary>
        public   abstract   IReadOnlyList<MessageDef>   Commands        { get; }
        /// <summary>
        /// If true the <see cref="TypeDef"/> is the root type of a database schema.<br/>
        /// Each field in <see cref="Fields"/> defines a database container / table.<br/>
        /// It also declare list of <see cref="Commands"/> and <see cref="Messages"/> exposed by a <c>DatabaseService</c>.
        /// </summary>
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
        public   readonly   string                      doc;
        /// <summary>meta data assigned to a schema compatible to <b>OpenAPI</b></summary>
        public              SchemaInfo                  SchemaInfo      => schemaInfo;
        public              bool                        IsEntity        => keyField != null;
        
        // --- internal
        internal readonly   string                      fullName;
        // can be null. If null keyField name of an entity defaults to "id"
        private  readonly   string                      keyFieldName;
        private             FieldDef                    keyField;
        /// <summary>Map of class fields if <see cref="TypeDef"/> is a class. Otherwise null</summary>
        private             Dictionary<string,FieldDef> fieldMap;
        internal            SchemaInfo                  schemaInfo;
        
        public const string DefaultNamespace = "Default";

        
        protected TypeDef (string name, string @namespace, string keyField, StandardTypeId typeId, string doc, in Utf8String nameUtf8) {
            if (@namespace == null) throw new ArgumentNullException(nameof(@namespace));
            fullName        = @namespace + "#" + name;
            Name            = name;
            this.nameUtf8   = nameUtf8;
            Namespace       = @namespace;
            keyFieldName    = keyField;
            this.TypeId     = typeId;
            this.doc        = doc;
            // if (nameUtf8.GetName() != name) throw new InvalidOperationException($"invalid UTF-8 name. Expect: {name}, was: {nameUtf8.ToString()}");
        }
        
        internal void SetFieldMap() {
            var fields  = Fields;
            fieldMap    = new Dictionary<string, FieldDef>(fields.Count);
            foreach (var field in fields) {
                fieldMap.Add(field.name, field);
            }
        }
        
        public FieldDef FindField(string name) {
            if (fieldMap == null) throw new InvalidOperationException($"TypeDef {Name} is not a class.");
            fieldMap.TryGetValue(name, out var field);
            return field;
        }
        
        public void GetDependencies(HashSet<TypeDef> dependencies) {
            if (!dependencies.Add(this))
                return;
            if (IsClass) {
                foreach (var field in Fields) {
                    var fieldType = field.type;
                    fieldType.GetDependencies(dependencies);
                    var fieldRelationType = field.RelationType;
                    fieldRelationType?.GetDependencies(dependencies);
                }
            }
            var baseType = BaseType;
            baseType?.GetDependencies(dependencies);
            var unionType = UnionType;
            if (unionType != null) {
                foreach (var polyType in unionType.types) {
                    polyType.typeDef.GetDependencies(dependencies);
                }
            }
        }
        
        internal void  SetEntityKeyFields() {
            if (!IsSchema) {
                return;
            }
            foreach (var field in Fields) {
                var fieldType       = field.type;
                var keyName         = fieldType.keyFieldName ?? "id";
                fieldType.keyField  = fieldType.FindField(keyName);
                if (fieldType.keyField == null) throw new NullReferenceException("keyField");
            }
        }
    }
    
    // could by a readonly struct - but may be used by reference in future
    /// <summary>
    /// The type definition of field (also named property) in a <see cref="TypeDef"/>. E.g. a scalar type like boolean,
    /// int, float, double, DateTime, Guid, BigInteger or string or a complex type like an array, a map (= Dictionary) or a class.
    /// Fields also have a modifier to specify if a field is required or optional.
    /// <br/>
    /// Within a database schema every table / container has a primary key exposed by the <see cref="TypeDef.KeyField"/>. 
    /// </summary>
    public sealed class FieldDef {
        public   readonly   string          name;
        public   readonly   string          nativeName;
        public   readonly   Utf8String      nameUtf8;
        public   readonly   bool            required;
        public   readonly   bool            isAutoIncrement;
        public   readonly   TypeDef         type;
        public   readonly   PropField       propField;
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
            string      nativeName,
            bool        required,
            bool        isAutoIncrement,
            TypeDef     type,
            PropField   propField,
            bool        isArray,
            bool        isDictionary,
            bool        isNullableElement,
            TypeDef     ownerType,
            string      relation,
            string      doc,
            IUtf8Buffer buffer) 
        {
            this.name               = name;
            this.nativeName         = nativeName;
            this.nameUtf8           = buffer.GetOrAdd(name);
            this.required           = required;
            this.isAutoIncrement    = isAutoIncrement;
            this.type               = type;
            this.propField          = propField;
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
        public   readonly   Utf8String  nameUtf8;
        public   readonly   string      doc;

        public   override   string      ToString() => name;

        private EnumValue (string name, string doc, IUtf8Buffer buffer) {
            this.name       = name;
            this.nameUtf8   = buffer.Add(name);
            this.doc        = doc;
        }
        
        internal static List<EnumValue> CreateEnumValues(
            IReadOnlyList<string>               enumNames,
            IReadOnlyDictionary<string,string>  enumDocs,
            IUtf8Buffer                         buffer)
        {
            if (enumNames == null)
                return null;
            var enumValues  = new List<EnumValue>(enumNames.Count);
            foreach (var name in enumNames) {
                string doc = null; 
                enumDocs?.TryGetValue(name, out doc);
                enumValues.Add(new EnumValue(name, doc, buffer));
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
        public   readonly   Utf8String                  discriminatorUtf8;
        public   readonly   string                      doc;
        public   readonly   IReadOnlyList<UnionItem>    types;
        
        public   override   string                      ToString() => discriminator;
        
        public UnionType(string discriminator, string doc, List<UnionItem> types, IUtf8Buffer buffer) {
            this.discriminator      = discriminator;
            this.discriminatorUtf8  = buffer.Add(discriminator);
            this.doc                = doc;
            this.types              = types;
        }
    }
    
    public readonly struct UnionItem
    {
        public   readonly   TypeDef     typeDef;
        public   readonly   string      discriminant;
        public   readonly   Utf8String  discriminantUtf8;

        public   override   string      ToString() => discriminant;

        public UnionItem (TypeDef typeDef, string discriminant, IUtf8Buffer buffer) {
            this.typeDef            = typeDef;
            this.discriminant       = discriminant ?? throw new ArgumentNullException(nameof(discriminant));
            this.discriminantUtf8   = buffer.Add(discriminant);
        }
    }
}