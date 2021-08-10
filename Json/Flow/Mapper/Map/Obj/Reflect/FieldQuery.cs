// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Friflo.Json.Flow.Mapper.Map.Utils;

namespace Friflo.Json.Flow.Mapper.Map.Obj.Reflect
{
    public class  FieldQuery
    {
        internal readonly   List<PropField>     fieldList = new List <PropField>();
        internal            int                 primCount;
        internal            int                 objCount;
        private  readonly   TypeStore           typeStore;

        internal FieldQuery(TypeStore typeStore, Type type) {
            this.typeStore = typeStore;
            TraverseMembers(type, true);
        }

        private void CreatePropField (Type type, string fieldName, PropertyInfo property, FieldInfo field, bool addMembers) {
            // getter have higher priority than fields with the same fieldName. Same behavior as other serialization libs
            Type            memberType;
            string          jsonName;
            bool            required;
            if (property != null) {
                memberType   = property.PropertyType;
                AttributeUtils.Property(property.CustomAttributes, out jsonName, out required);
                if (property.GetSetMethod(false) == null)
                    required = true;
            } else {
                memberType   = field.FieldType;
                AttributeUtils.Property(field.CustomAttributes, out jsonName, out required);
                // used for fields like: readonly EntitySet<Order>
                if ((field.Attributes & FieldAttributes.InitOnly) != 0)
                    required = true;
            }
            if (memberType == null)
                throw new InvalidOperationException("Field '" + fieldName + "' ('" + fieldName + "') not found in type " + type);

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
                
                PropField pf;
                if (memberType.IsEnum || memberType.IsPrimitive || isNullablePrimitive || isNullableEnum) {
                    pf =     new PropField(fieldName, jsonName, mapper, field, property, primCount,    -9999, required); // force index exception in case of buggy impl.
                } else {
                    if (mapper.isValueType)
                        pf = new PropField(fieldName, jsonName, mapper, field, property, primCount, objCount, required);
                    else
                        pf = new PropField(fieldName, jsonName, mapper, field, property, -9999,     objCount, required); // force index exception in case of buggy impl.
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
                // Is getter and setter public?
                bool isPublic = property.GetGetMethod(false) != null && property.GetSetMethod(false) != null;
                if (!isPublic && !Property(property.CustomAttributes))
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
                if (!field.IsPublic && !Property(field.CustomAttributes))
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
        
        private static bool Property(IEnumerable<CustomAttributeData> attributes) {
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(Fri.PropertyAttribute))
                    return true;
            }
            return false;
        }
        
        private static bool Ignore(IEnumerable<CustomAttributeData> attributes) {
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(Fri.IgnoreAttribute))
                    return true;
            }
            return false;
        }
    }
    
    public static class AttributeUtils {
                
        public static void Property(IEnumerable<CustomAttributeData> attributes, out string name, out bool required) {
            name        = null;
            required    = false;
            foreach (var attr in attributes) {
                if (attr.AttributeType != typeof(Fri.PropertyAttribute))
                    continue;
                if (attr.NamedArguments == null)
                    continue;
                foreach (var args in attr.NamedArguments) {
                    switch (args.MemberName) {
                        case nameof(Fri.PropertyAttribute.Name):
                            if (args.TypedValue.Value != null)
                                name = args.TypedValue.Value as string;
                            break;
                        case nameof(Fri.PropertyAttribute.Required):
                            if (args.TypedValue.Value != null)
                                required = (bool)args.TypedValue.Value;
                            break;
                    }
                }
            }
        }
    }
}
