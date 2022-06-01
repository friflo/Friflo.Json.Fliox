// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Obj.Reflect
{
    public sealed class  FieldQuery
    {
        internal readonly   List<PropField>     fieldList = new List <PropField>();
        internal            int                 primCount;
        internal            int                 objCount;
        private  readonly   TypeStore           typeStore;
        private  readonly   FieldFilter         fieldFilter;

        internal FieldQuery(TypeStore typeStore, Type type, FieldFilter fieldFilter) {
            this.typeStore      = typeStore;
            this.fieldFilter    = fieldFilter;
            TraverseMembers(type, true);
        }

        private void CreatePropField (Type type, string fieldName, PropertyInfo property, FieldInfo field, bool addMembers) {
            // getter have higher priority than fields with the same fieldName. Same behavior as other serialization libs
            Type            memberType;
            string          jsonName;
            bool            required;
            MemberInfo      memberInfo;
            string          docPrefix;
            if (property != null) {
                memberInfo   = property;
                docPrefix    = "P:";
                memberType   = property.PropertyType;
                AttributeUtils.Property(property.CustomAttributes, out jsonName);
                required = IsRequired(property.CustomAttributes);
                if (property.GetSetMethod(false) == null)
                    required = true;
            } else {
                memberInfo   = field;
                docPrefix    = "F:";
                memberType   = field.FieldType;
                AttributeUtils.Property(field.CustomAttributes, out jsonName);
                required = IsRequired(field.CustomAttributes);
                // used for fields like: readonly EntitySet<Order>
                if ((field.Attributes & FieldAttributes.InitOnly) != 0)
                    required = true;
            }
            if (memberType == null)
                throw new InvalidOperationException("Field '" + fieldName + "' ('" + fieldName + "') not found in type " + type);

            try {
                TypeMapper  mapper      = typeStore.GetTypeMapper(memberType);
                /* var refMapper = EntityMatcher.GetRefMapper(memberType, typeStore.config, mapper);
                if (refMapper != null)
                    mapper = refMapper; */

                Type        ut          = mapper.nullableUnderlyingType;
                bool isNullablePrimitive = ut != null && ut.IsPrimitive;
                bool isNullableEnum      = ut != null && ut.IsEnum;
                
                if (addMembers) {
                    if (jsonName == null)
                        jsonName = typeStore.config.jsonNaming.PropertyName(fieldName);
                    
                    string docs         = null;
                    var assemblyDocs    = typeStore.assemblyDocs;
                    var declaringType   = memberInfo.DeclaringType;
                    if (assemblyDocs != null && declaringType != null) {
                        var signature  = $"{docPrefix}{declaringType.FullName}.{fieldName}";
                        docs           = assemblyDocs.GetDocs(declaringType.Assembly, signature);
                    }
                    PropField pf;
                    if (memberType.IsEnum || memberType.IsPrimitive || isNullablePrimitive || isNullableEnum) {
                        pf =     new PropField(fieldName, jsonName, mapper, field, property, primCount,    -9999, required, docs); // force index exception in case of buggy impl.
                    } else {
                        if (mapper.isValueType)
                            pf = new PropField(fieldName, jsonName, mapper, field, property, primCount, objCount, required, docs);
                        else
                            pf = new PropField(fieldName, jsonName, mapper, field, property, -9999,     objCount, required, docs); // force index exception in case of buggy impl.
                    }

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

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            PropertyInfo[] properties = type.GetProperties(flags);
            for (int n = 0; n < properties.Length; n++) {
                var property = properties[n];
                if (Ignore(property.CustomAttributes))
                    continue;
                if (!(property.CanRead && property.CanWrite))
                    continue;
                if (!fieldFilter.AddField(property))
                    continue;
                var name = property.Name;
                CreatePropField(type, name, property, null, addMembers);
            }

            FieldInfo[] fields = type.GetFields(flags);
            for (int n = 0; n < fields.Length; n++) {
                var field = fields[n];
                if (Ignore(field.CustomAttributes))
                    continue;
                if (IsAutoGeneratedBackingField(field))
                    continue;
                if (!fieldFilter.AddField(field))
                    continue;
                var name = field.Name;
                CreatePropField(type, name, null, field, addMembers);
            }
        }

        private static bool IsAutoGeneratedBackingField(FieldInfo field) {
            foreach (CustomAttributeData attr in field.CustomAttributes) {
                if (attr.AttributeType == typeof(CompilerGeneratedAttribute))
                    return true;
            }
            return false;
        }
        
        public static bool Property(IEnumerable<CustomAttributeData> attributes) {
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(Fri.PropertyMemberAttribute))
                    return true;
            }
            return false;
        }
        
        private static bool Ignore(IEnumerable<CustomAttributeData> attributes) {
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(Fri.IgnoreMemberAttribute))
                    return true;
            }
            return false;
        }


        private static bool IsRequired(IEnumerable<CustomAttributeData> attributes) {
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(Fri.RequiredMemberAttribute))
                    return true;
                // Unity has System.ComponentModel.DataAnnotations.KeyAttribute no available by default
                if (attr.AttributeType.FullName == "System.ComponentModel.DataAnnotations.RequiredAttribute")
                    return true;
            }
            return false;
        }
        
        public static bool IsKey(IEnumerable<CustomAttributeData> attributes) {
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(Fri.PrimaryKeyAttribute))
                    return true;
                // Unity has System.ComponentModel.DataAnnotations.KeyAttribute no available by default
                if (attr.AttributeType.FullName == "System.ComponentModel.DataAnnotations.KeyAttribute")
                    return true;
            }
            return false;
        }
        
        public static bool IsAutoIncrement(IEnumerable<CustomAttributeData> attributes) {
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(Fri.AutoIncrementAttribute))
                    return true;
            }
            return false;
        }
        
        internal static readonly FieldFilter DefaultMemberFilter = new FieldFilter();
    }
    
    public class FieldFilter
    {
        public virtual bool AddField(MemberInfo memberInfo) {
            if (memberInfo is PropertyInfo property) {
                // has public getter and setter?
                bool hasPublicGetSet = property.GetGetMethod(false) != null && property.GetSetMethod(false) != null;
                return hasPublicGetSet || FieldQuery.Property(property.CustomAttributes);
            }
            if (memberInfo is FieldInfo field) {
                return field.IsPublic || FieldQuery.Property(field.CustomAttributes);
            }
            return false;
        }
    }
}
