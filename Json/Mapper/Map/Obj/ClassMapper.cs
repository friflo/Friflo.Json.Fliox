// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Map.Val;
using Friflo.Json.Mapper.MapIL.Obj;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Obj
{
    public class ClassMatcher : ITypeMatcher {
        public static readonly ClassMatcher Instance = new ClassMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type nullableStruct = TypeUtils.GetNullableStruct(type);
            if (nullableStruct == null && TypeUtils.IsGenericType(type)) // dont handle generic types like List<> or Dictionary<,>
                return null;
            if (EnumMatcher.IsEnum(type, out bool _))
                return null;
           
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            if (type.IsClass || type.IsValueType) {
                object[] constructorParams = {type, constructor, type.IsValueType};
#if !UNITY_5_3_OR_NEWER
                if (config.useIL) {
                    if (type.IsValueType) {
                        // new StructMapper<T>(type, constructor);
                        return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(StructILMapper<>),
                            new[] {type}, constructorParams);
                    }
                    // new ClassILMapper<T>(type, constructor);
                    return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(ClassILMapper<>), new[] {type}, constructorParams);
                }
#endif
                // new ClassMapper<T>(type, constructor);
                return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(ClassMapper<>), new[] {type}, constructorParams);
            }
            return null;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ClassMapper<T> : TypeMapper<T> {
        private   readonly Dictionary <string, PropField> strMap      = new Dictionary <string, PropField>(13);
        private   readonly HashMapOpen<Bytes,  PropField> fieldMap;
        // ReSharper disable once UnassignedReadonlyField - field ist set via reflection below to use make field readonly
        protected readonly PropertyFields                 propFields;
        private   readonly ConstructorInfo                constructor;
        private   readonly Bytes                          removedKey;
        
        public override string DataTypeName() { return "class"; }

       
        protected ClassMapper (Type type, ConstructorInfo constructor, bool isValueType) :
            base (type, IsNullable(type), isValueType)
        {
            removedKey = new Bytes("__REMOVED");
            fieldMap = new HashMapOpen<Bytes, PropField>(11, removedKey);
            this.constructor = constructor;
        }
        
        public override void Dispose() {
            base.Dispose();
            propFields.Dispose();
            removedKey.Dispose();
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            var fields = new PropertyFields(type, typeStore);
            FieldInfo fieldInfo = typeof(ClassMapper<T>).GetField(nameof(propFields), BindingFlags.NonPublic | BindingFlags.Instance);
            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(this, fields);
            
            for (int n = 0; n < propFields.num; n++) {
                PropField   field = propFields.fields[n];
                if (strMap.ContainsKey(field.name))
                    throw new InvalidOperationException("assert field is accessible via string lookup");
                strMap.Add(field.name, field);
                fieldMap.Put(ref field.nameBytes, field);
            }
        }
        
        private static bool IsNullable(Type type) {
            if (!type.IsValueType)
                return true;
            return TypeUtils.GetNullableStruct (type) != null;
        }
        
        public override object CreateInstance()
        {
            if (constructor == null) {
                // Is it a struct?
                if (type.IsValueType)
                    return Activator.CreateInstance(type);
                throw new InvalidOperationException("No default constructor available for: " + type.Name);
            }
            return ReflectUtils.CreateInstance(constructor);
        }

        public override PropField GetField (ref Bytes fieldName) {
            // Note: its likely that hashcode ist not set properly. So calculate anyway
            fieldName.UpdateHashCode();
            PropField pf = fieldMap.Get(ref fieldName);
            if (pf == null)
                Console.Write("");
            return pf;
        }
        
        public override PropertyFields GetPropFields() {
            return propFields;
        }
        
        
        // ----------------------------------- Write / Read -----------------------------------
        
        public override void Write(JsonWriter writer, T slot) {
            int startLevel = WriteUtils.IncLevel(writer);
            ref var bytes = ref writer.bytes;
            T obj = slot;
            TypeMapper classMapper = this;
            bool firstMember = true;
            bytes.AppendChar('{');

            Type objType = obj.GetType(); // GetType() cost performance. May use a pre-check with isPolymorphic  
            if (type != objType) {
                classMapper = writer.typeCache.GetTypeMapper(objType);
                firstMember = false;
                bytes.AppendBytes(ref writer.discriminator);
                writer.typeCache.AppendDiscriminator(ref bytes, classMapper);
                bytes.AppendChar('\"');
            }

            PropField[] fields = classMapper.GetPropFields().fields;
            for (int n = 0; n < fields.Length; n++) {
                if (firstMember)
                    firstMember = false;
                else
                    bytes.AppendChar(',');
                PropField field = fields[n];
                WriteUtils.WriteKey(writer, field);
                
                object elemVar = field.GetField(obj);
                if (elemVar == null) {
                    WriteUtils.AppendNull(writer);
                } else {
                    var fieldType = field.fieldType;
                    fieldType.WriteObject(writer, elemVar);
                }
            }
            bytes.AppendChar('}');
            WriteUtils.DecLevel(writer, startLevel);
        }


        protected static TypeMapper GetPolymorphType(JsonReader reader, TypeMapper classType, ref T obj, out bool success) {
            ref var parser = ref reader.parser;
            var ev = parser.NextEvent();

            // Is first member is discriminator - "$type": "<typeName>" ?
            if (ev == JsonEvent.ValueString && reader.discriminator.IsEqualBytes(ref parser.key)) {
                classType = reader.typeCache.GetTypeByName(ref parser.value);
                if (classType == null)
                    return ReadUtils.ErrorMsg<TypeMapper>(reader, $"Object with discriminator {reader.discriminator} not found: ",ref parser.value, out success);
                parser.NextEvent();
            }
            if (classType.IsNull(ref obj))
                obj = (T)classType.CreateInstance();
            success = true;
            return classType;
        }

        public override T Read(JsonReader reader, T slot, out bool success) {
            // Ensure preconditions are fulfilled
            if (!ObjectUtils.StartObject(reader, this, out success))
                return default;
                
            T obj = slot;
            TypeMapper classType = this;
            classType = GetPolymorphType(reader, classType, ref obj, out success);
            if (!success)
                return default;
            
            ref var parser = ref reader.parser;
            JsonEvent ev = parser.Event;

            while (true) {
                object elemVar;
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        PropField field;
                        if ((field = ObjectUtils.GetField(reader, classType)) == null)
                            break;
                        TypeMapper fieldType = field.fieldType;
                        elemVar = fieldType.ReadObject(reader, null, out success);
                        if (!success)
                            return default;
                        field.SetField(obj, elemVar); // set also to null in error case
                        break;
                    case JsonEvent.ValueNull:
                        if ((field = ObjectUtils.GetField(reader, classType)) == null)
                            break;
                        if (!field.fieldType.isNullable) {
                            ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, field.fieldType, ref parser, out success);
                            return default;
                        }
                        field.SetField(obj, null);
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        if ((field = ObjectUtils.GetField(reader, classType)) == null)
                            break;
                        fieldType = field.fieldType;
                        elemVar = field.GetField(obj);
                        object sub = elemVar;
                        elemVar = fieldType.ReadObject(reader, elemVar, out success);
                        if (!success)
                            return default;
                        //
                        object subRet = elemVar;
                        if (!fieldType.isNullable && subRet == null) {
                            ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, fieldType, ref parser, out success);
                            return default;
                        }
                        if (sub != subRet)
                            field.SetField(obj, elemVar);
                        break;
                    case JsonEvent.ObjectEnd:
                        success = true;
                        return obj;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<T>(reader, "unexpected state: ", ev, out success);
                }
                ev = parser.NextEvent();
            }
        }
    }
}