// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Native
{
    /// <summary>
    /// <see cref="NativeTypeSchema"/> is used to create an immutable <see cref="TypeSchema"/> instance
    /// from a C# .NET <see cref="Type"/> passed to its constructor <see cref="NativeTypeSchema(Type)"/>
    /// </summary>
    public sealed class NativeTypeSchema : TypeSchema
    {
        public   override   IReadOnlyList<TypeDef>  Types           { get; }
        public   override   StandardTypes           StandardTypes   { get; }
        public   override   TypeDef                 RootType        { get; }
        
        public              TypeDef                 TypeAsTypeDef(Type type) => nativeTypes[type];
        internal            NativeTypeDef           GetNativeType(Type type) => nativeTypes[type];
        public   override   string                  ToString()               => nativeRootType.ToString();

        /// <summary>Contains only non <see cref="Nullable"/> Type's</summary>
        private  readonly   Type                            nativeRootType;
        private  readonly   Dictionary<Type, NativeTypeDef> nativeTypes;
        
        /// <summary> <see cref="NativeTypeSchema"/> instances are immutable so caching has no side effects </summary>
        private static readonly Dictionary<Type, NativeTypeSchema> Cache = new Dictionary<Type, NativeTypeSchema>();
        
        public  static  void                ClearCache () => Cache.Clear();
        public  static  NativeTypeSchema    Create     (Type rootType) {
            if (rootType == null) throw new ArgumentNullException(nameof(rootType));
            var cache = Cache;
            lock (cache) {
                if (!cache.TryGetValue(rootType, out var schema)) {
                    schema = new NativeTypeSchema(rootType);
                    cache.Add(rootType, schema);
                }
                return schema;
            }
        }

        private NativeTypeSchema (Type rootType)
        {
            nativeRootType  = rootType ?? throw new ArgumentNullException(nameof(rootType));
            var typeList    = new List<Type> {rootType};

            using (var typeStore = new TypeStore()) {
              
                typeStore.AddMappers(typeList);
                var typeMappers = typeStore.GetTypeMappers();
                TypeMapper rootTypeMapper = typeMappers[rootType];

                // Collect all types into containers to simplify further processing
                nativeTypes     = new Dictionary<Type, NativeTypeDef>(typeMappers.Count);
                var types       = new List<TypeDef>                  (typeMappers.Count);
                foreach (var pair in typeMappers) {
                    TypeMapper  mapper  = pair.Value;
                    AddType(types, mapper, typeStore);
                }
                /* typeMappers = typeStore.GetTypeMappers();
                foreach (var pair in typeMappers) {
                    TypeMapper  mapper  = pair.Value;
                    if (mapper.type == typeof(Guid?)) {
                        int x = 1;
                    }
                    if (mapper.type == typeof(Guid)) {
                        int x = 1;
                    }
                    AddType(types, mapper, typeStore);
                } */
                // in case any Nullable<> was found - typeStore contain now also their non-nullable counterparts.
                typeMappers = typeStore.GetTypeMappers();
                
                var standardTypes = new NativeStandardTypes(nativeTypes);
                StandardTypes   = standardTypes;

                // set the base type (base class or parent class) for all types. 
                foreach (var pair in nativeTypes) {
                    NativeTypeDef   typeDef     = pair.Value;
                    Type            baseType    = typeDef.native.BaseType;
                    TypeMapper      mapper;
                    // When searching for polymorph base class there may be are classes in this hierarchy. E.g. BinaryBoolOp. 
                    // If these classes may have a protected constructor they need to be skipped. These classes have no TypeMapper. 
                    while (!typeMappers.TryGetValue(baseType, out  mapper)) {
                        baseType = baseType.BaseType;
                        if (baseType == null)
                            break;
                    }
                    if (mapper != null) {
                        typeDef.baseType = nativeTypes[mapper.type];
                    }
                }
                foreach (var pair in nativeTypes) {
                    NativeTypeDef   typeDef = pair.Value;
                    TypeMapper      mapper  = typeDef.mapper;
                    
                    // set the fields for classes or structs
                    var  propFields         = mapper.PropFields;
                    if (propFields != null) {
                        typeDef.keyField        = propFields.GetPropField("id") != null ? "id" : null;
                        typeDef.fields = new List<FieldDef>(propFields.fields.Length);
                        foreach (var propField in propFields.fields) {
                            var fieldMapper     = propField.fieldType.GetUnderlyingMapper();
                            var isNullable      = IsNullableMapper(fieldMapper, out var nonNullableType) ||
                                                  fieldMapper.type == typeof(JsonValue);
                            var isArray         = fieldMapper.IsArray;
                            var isDictionary    = fieldMapper.IsDictionary;
                            NativeTypeDef type;
                            bool isNullableElement = false;
                            if (isArray || isDictionary) {
                                var elementMapper       = fieldMapper.GetElementMapper();
                                var underlyingMapper    = elementMapper.GetUnderlyingMapper();
                                if(underlyingMapper.isValueType && underlyingMapper.isNullable) {
                                    IsNullableMapper(underlyingMapper, out var nonNullableElementType);
                                    type = nativeTypes[nonNullableElementType];
                                    isNullableElement = true;
                                } else {
                                    type = nativeTypes[underlyingMapper.type];
                                }
                            } else {
                                type            = nativeTypes[nonNullableType];
                            }
                            var relation    = propField.relation;
                            var required    = propField.required || !isNullable;
                            var isKey       = propFields.KeyField == propField;
                            if (isKey) {
                                typeDef.keyField = propField.jsonName;
                            }
                            bool isAutoIncrement = AttributeUtils.IsAutoIncrement(propField.Member.CustomAttributes);

                            var fieldDef = new FieldDef (propField.jsonName, propField.name, required, isKey, isAutoIncrement, type,
                                isArray, isDictionary, isNullableElement, typeDef, relation, propField.docs, Utf8Buffer);
                            typeDef.fields.Add(fieldDef);
                        }
                    }
                    var commands = HubMessagesUtils.GetMessageInfos(typeDef.native, typeStore);
                    AddMessages(typeDef, commands);

                    if (typeDef.Discriminant != null) {
                        var baseType = typeDef.baseType;
                        while (baseType != null) {
                            var unionType = baseType.unionType;
                            if (unionType != null) {
                                typeDef.discriminator       = unionType.discriminator;
                                typeDef.discriminatorDoc    = unionType.doc;
                                break;
                            }
                            baseType = baseType.baseType;
                        }
                        if (typeDef.discriminator == null)
                            throw new InvalidOperationException($"found no discriminator in base classes. type: {typeDef}");
                    }
                    // set the unionType if a class is a discriminated union
                    var factory = mapper.instanceFactory;
                    if (factory != null) {
                        typeDef.isAbstract = true;
                        // expect polyTypes if not abstract
                        if (!factory.isAbstract) {
                            var polyTypes   = factory.polyTypes;
                            var unionTypes  = new List<UnionItem>(polyTypes.Length);
                            foreach (var polyType in polyTypes) {
                                TypeDef element = nativeTypes[polyType.type];
                                var item = new UnionItem (element, polyType.name, Utf8Buffer);
                                unionTypes.Add(item);
                            }
                            typeDef.unionType  = new UnionType (factory.discriminator, factory.description, unionTypes, Utf8Buffer);
                        }
                    }
                }
                MarkDerivedFields(types);
                // nativeTypes contain only non-nullable types => no entries for int?, double?, ...
                if (nativeTypes.TryGetValue(rootType, out var rootTypeDef)) {
                    SetKeyField(rootTypeDef);
                    SetRelationTypes(rootTypeDef, types);
                    RootType = rootTypeDef;
                    MarkEntityTypes();
                    rootTypeDef.schemaInfo    = SchemaInfo.GetSchemaInfo(rootType);
                }
                Types = OrderTypes(RootType, types);
            }
        }
        
        private void AddMessages(NativeTypeDef typeDef, MessageInfo[] messageInfos) {
            if (messageInfos == null || messageInfos.Length == 0)
                return;
            var messageDefs = typeDef.messages;
            var commandDefs = typeDef.commands;
            foreach (var command in messageInfos) {
                var paramArg    = GetMessageArg("param",   command.paramType,  false);
                if (command.resultType != null) {
                    var resultArg   = GetMessageArg("result",  command.resultType, true);
                    var commandDef  = new MessageDef(command.name, paramArg, resultArg, command.doc);
                    if (commandDefs == null)
                        commandDefs = typeDef.commands = new List<MessageDef>();
                    commandDefs.Add(commandDef);
                } else {
                    var messageDef  = new MessageDef(command.name, paramArg, null, command.doc);
                    if (messageDefs == null)
                        messageDefs = typeDef.messages = new List<MessageDef>();
                    messageDefs.Add(messageDef);
                }
            }
        }
        
        private FieldDef GetMessageArg(string name, Type type, bool required) {
            if (type == null)
                return null;
            var attr        = GetArgAttributes(type);
            required       |= attr.required;
            return new FieldDef(name, null, required, false, false, attr.typeDef, false, false, false, null, null, null, Utf8Buffer);
        }
        
        private void AddType(List<TypeDef> types, TypeMapper typeMapper, TypeStore typeStore) {
            var mapper  = typeMapper.GetUnderlyingMapper();
            if (IsNullableMapper(mapper, out var nonNullableType)) {
                mapper = typeStore.GetTypeMapper(nonNullableType);
            }
            if (nativeTypes.ContainsKey(nonNullableType))
                return;
            NativeTypeDef typeDef;
            if (NativeStandardTypes.Types.TryGetValue(nonNullableType, out string name)) {
                typeDef = new NativeTypeDef(mapper, name, "Standard", Utf8Buffer);
            } else {
                typeDef = new NativeTypeDef(mapper, nonNullableType.Name, nonNullableType.Namespace, Utf8Buffer);
            }
            nativeTypes.Add(nonNullableType, typeDef);
            types.      Add(typeDef);
            
            var baseType = mapper.BaseType;
            if (baseType != null) {
                var baseMapper = typeStore.GetTypeMapper(baseType);
                AddType(types, baseMapper, typeStore);
            }
            /* var instanceFactory = mapper.instanceFactory;
            if (instanceFactory != null) {
                // expect polyTypes if not abstract
                if (!instanceFactory.isAbstract) {
                    var polyTypes   = instanceFactory.polyTypes;
                    foreach (var polyType in polyTypes) {
                        var polyTypeDef = typeStore.GetTypeMapper(polyType.type);
                        AddType(types, polyTypeDef, typeStore);
                    }
                }
            } */
        }
        
        private static bool IsNullableMapper(TypeMapper mapper, out Type nonNullableType) {
            var isNullable = mapper.isNullable;
            if (isNullable && mapper.nullableUnderlyingType != null) {
                nonNullableType = mapper.nullableUnderlyingType;
                return true;
            }
            nonNullableType = mapper.type;
            return isNullable;
        }
        
        public ICollection<TypeDef> TypesAsTypeDefs(ICollection<Type> types) {
            if (types == null)
                return null;
            var list = new List<TypeDef> (types.Count);
            foreach (var nativeType in types) {
                var type = nativeTypes[nativeType];
                list.Add(type);
            }
            return list;
        }
        
        private ArgAttributes GetArgAttributes(Type type) {
            var underlyingType  = Nullable.GetUnderlyingType(type);
            if (underlyingType != null) {
                var typeDef     = nativeTypes[underlyingType];
                return new ArgAttributes(false, typeDef);
            } else {
                var typeDef     = nativeTypes[type];
                bool required   = !type.IsClass;
                return new ArgAttributes(required, typeDef);
            }
        }
    }
    
    internal readonly struct ArgAttributes
    {
        internal  readonly  bool            required;
        internal  readonly  NativeTypeDef   typeDef;
        
        internal ArgAttributes (bool required, NativeTypeDef typeDef) {
            this.required       = required;
            this.typeDef        = typeDef;
        }
    }
}