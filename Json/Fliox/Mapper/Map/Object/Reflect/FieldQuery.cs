// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Object.Reflect
{
    public abstract class FieldQuery
    {
        internal  readonly  List<PropField>     fields = new List <PropField>();
        internal            int                 primCount;
        internal            int                 objCount;
        internal            int                 fieldCount;
        protected readonly  TypeStore           typeStore;
        protected readonly  FieldFilter         fieldFilter;
        internal  readonly  Type                type;
        internal  readonly  Type                genClass;
        
        internal FieldQuery(TypeStore typeStore, Type type, Type genClass, FieldFilter fieldFilter) {
            this.typeStore      = typeStore;
            this.fieldFilter    = fieldFilter;
            this.type           = type;
            this.genClass       = genClass;
        }
    } 
    
    public sealed class  FieldQuery<T> : FieldQuery
    {
        internal readonly   List<PropField<T>>  fieldList = new List <PropField<T>>();

        public FieldQuery(TypeStore typeStore, Type type, Type genClass, FieldFilter fieldFilter = null)
            : base(typeStore, type, genClass, fieldFilter ?? FieldFilter.DefaultMemberFilter)
        {
            TraverseMembers(type, true);
            foreach (var field in fieldList) {
                fields.Add(field);
            }
        }
        
        private void CreatePropField (Type type, string fieldName, PropertyInfo property, FieldInfo field, bool addMembers) {
            // getter have higher priority than fields with the same fieldName. Same behavior as other serialization libs
            Type            memberType;
            bool            required;
            MemberInfo      memberInfo;
            string          docPrefix;
            if (property != null) {
                memberInfo   = property;
                docPrefix    = "P:";
                memberType   = property.PropertyType;
                required    = AttributeUtils.IsRequired(property.CustomAttributes);
                if (property.GetSetMethod(false) == null) {
                    required = true;
                }
            } else {
                memberInfo   = field;
                docPrefix    = "F:";
                memberType   = field.FieldType;
                required    = AttributeUtils.IsRequired(field.CustomAttributes);
                // used for fields like: readonly EntitySet<Order>
                if ((field.Attributes & FieldAttributes.InitOnly) != 0) {
                    required = true;
                }
            }
            if (memberType == null) {
                throw new InvalidOperationException("Field '" + fieldName + "' ('" + fieldName + "') not found in type " + type);
            }
            try {
                TypeMapper  mapper      = typeStore.GetTypeMapper(memberType);
                /* var refMapper = EntityMatcher.GetRefMapper(memberType, typeStore.config, mapper);
                if (refMapper != null)
                    mapper = refMapper; */

                Type        ut          = mapper.nullableUnderlyingType;
                bool isNullablePrimitive = ut != null && ut.IsPrimitive;
                bool isNullableEnum      = ut != null && ut.IsEnum;
                
                if (addMembers) {
                    var jsonName        = AttributeUtils.GetMemberJsonName(memberInfo);
                    string docs         = null;
                    var assemblyDocs    = typeStore.assemblyDocs;
                    var declaringType   = memberInfo.DeclaringType;
                    if (assemblyDocs != null && declaringType != null) {
                        var signature  = $"{docPrefix}{declaringType.FullName}.{fieldName}";
                        docs           = assemblyDocs.GetDocs(declaringType.Assembly, signature);
                    }
                    var genIndex = -1;
                    if (genClass != null) {
                        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                        var genIndexField = genClass.GetField($"Gen_{fieldName}", flags);
                        genIndex = (int)genIndexField.GetValue(null);
                    }
                    var member = PropField.CreateMember<T>(mapper, field, property);
                    PropField<T> pf;
                    if (memberType.IsEnum || memberType.IsPrimitive || isNullablePrimitive || isNullableEnum) {
                        pf =     new PropField<T>(fieldName, jsonName, mapper, field, property, member, fieldCount, genIndex, required, docs); // force index exception in case of buggy impl.
                    } else {
                        if (mapper.isValueType)
                            pf = new PropField<T>(fieldName, jsonName, mapper, field, property, member, fieldCount, genIndex, required, docs);
                        else
                            pf = new PropField<T>(fieldName, jsonName, mapper, field, property, member, fieldCount, genIndex, required, docs); // force index exception in case of buggy impl.
                    }
                    fieldCount++;
                    fieldList.Add(pf);
                }
                
                if (memberType.IsPrimitive || isNullablePrimitive || memberType.IsEnum || isNullableEnum) {
                    primCount++;
                } else if (mapper.isValueType) {
                    // struct itself must not be incremented only its members. Their position need to be counted 
                    TraverseMembers(mapper.type, false);
                } else
                    objCount++; // object
            } catch (InvalidTypeException e) {
                throw new InvalidTypeException($"Invalid member: {type.Name}.{fieldName} - {e.Message}");
            }
        }

        private void TraverseMembers(Type type, bool addMembers) {
            Type nullableStruct = TypeUtils.GetNullableStruct(type);
            if (nullableStruct != null) {
                type = nullableStruct;
                primCount++;  // require array element to represent if Nullable<struct> is null or set (1) 
            }
            var members = new List<MemberInfo>();
            AttributeUtils.GetMembers(type, members);
            foreach (var member in members) {
                switch (member) {
                    case PropertyInfo property:
                        if (AttributeUtils.Ignore(property.CustomAttributes))
                            continue;
                        if (!(property.CanRead && property.CanWrite))
                            continue;
                        if (!fieldFilter.AddField(property))
                            continue;
                        if (AttributeUtils.IsIndexerProperty(property))
                            continue;
                        var name = property.Name;
                        CreatePropField(type, name, property, null, addMembers);
                        break;
                    case FieldInfo field:
                        if (AttributeUtils.Ignore(field.CustomAttributes))
                            continue;
                        if (AttributeUtils.IsAutoGeneratedBackingField(field))
                            continue;
                        if (!fieldFilter.AddField(field))
                            continue;
                        name = field.Name;
                        CreatePropField(type, name, null, field, addMembers);
                        break;
                }
            }
        }
    }
    
    public class FieldFilter
    {
        internal  static readonly FieldFilter DefaultMemberFilter = new FieldFilter();

        public virtual bool AddField(MemberInfo memberInfo) {
            if (memberInfo is PropertyInfo property) {
                // has public getter and setter?
                bool hasPublicGetSet = property.GetGetMethod(false) != null && property.GetSetMethod(false) != null;
                return hasPublicGetSet || AttributeUtils.Property(property.CustomAttributes);
            }
            if (memberInfo is FieldInfo field) {
                return field.IsPublic || AttributeUtils.Property(field.CustomAttributes);
            }
            return false;
        }
    }
}
