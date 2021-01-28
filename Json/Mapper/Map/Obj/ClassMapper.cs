// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Class.IL;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Map.Val;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Obj
{
    public class ClassMatcher : ITypeMatcher {
        public static readonly ClassMatcher Instance = new ClassMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, ResolverConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            if (TypeUtils.IsGenericType(type)) // dont handle generic types like List<> or Dictionary<,>
                return null;
            if (EnumMatcher.IsEnum(type, out bool _))
                return null;
           
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            if (type.IsClass || type.IsValueType) {
                object[] constructorParams = {type, constructor};
                return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(ClassMapper<>), new[] {type}, constructorParams); // new ClassMapper<T>(type, constructor);
            }
            return null;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ClassMapper<T> : TypeMapper<T> {
        private readonly Dictionary <string, PropField> strMap      = new Dictionary <string, PropField>(13);
        private readonly HashMapOpen<Bytes,  PropField> fieldMap;
        private readonly PropertyFields                 propFields;
        private readonly ConstructorInfo                constructor;
        private readonly Bytes                          removedKey;
        private readonly ClassLayout                    layout;
        
        public override string DataTypeName() { return "class"; }

        public override ClassLayout GetClassLayout() { return layout; }
        
        public ClassMapper (Type type, ConstructorInfo constructor) :
            base (type, IsNullable(type))
        {
            removedKey = new Bytes("__REMOVED");
            fieldMap = new HashMapOpen<Bytes, PropField>(11, removedKey);

            propFields = new PropertyFields(type);
            for (int n = 0; n < propFields.num; n++) {
                PropField   field = propFields.fields[n];
                if (strMap.ContainsKey(field.name))
                    throw new InvalidOperationException("assert field is accessible via string lookup");
                strMap.Add(field.name, field);
                fieldMap.Put(ref field.nameBytes, field);
            }
            layout = new ClassLayout(type, propFields);
            this.constructor = constructor;
        }
        
        public override void Dispose() {
            base.Dispose();
            propFields.Dispose();
            removedKey.Dispose();
        }

        public override void InitTypeMapper(TypeStore typeStore) {
            for (int n = 0; n < propFields.num; n++) {
                PropField field = propFields.fields[n];

                var         mapper      = typeStore.GetTypeMapper(field.fieldTypeNative);
                FieldInfo   fieldInfo   = field.GetType().GetField(nameof(PropField.fieldType));
                // ReSharper disable once PossibleNullReferenceException
                fieldInfo.SetValue(field, mapper);
            }
        }
        
        private static bool IsNullable(Type type) {
            return !type.IsValueType;
        }
        
        public override object CreateInstance()
        {
            if (constructor == null) {
                // Is it a struct?
                if (type.IsValueType)
                    return Activator.CreateInstance(type);
                throw new FrifloException("No default constructor available for: " + type.Name);
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
            Type objType = obj.GetType();
            if (type != objType) {
                classMapper = writer.typeCache.GetTypeMapper(objType);
                firstMember = false;
                bytes.AppendBytes(ref writer.discriminator);
                writer.typeCache.AppendDiscriminator(ref bytes, classMapper);
                bytes.AppendChar('\"');
            }
            
            var payload = JsonWriter.InstanceLoad(writer, classMapper, obj);
            
            PropField[] fields = classMapper.GetPropFields().fieldsSerializable;
            for (int n = 0; n < fields.Length; n++) {
                if (firstMember)
                    firstMember = false;
                else
                    bytes.AppendChar(',');
                
                PropField field = fields[n];
                if (writer.useIL && field.isValueType) {
                    field.fieldType.WriteField(writer, ref payload, field);
                    continue;
                }                
                object elemVar = field.GetField(obj);
                WriteUtils.WriteKey(writer, field);
                // if (field.fieldType.varType == VarType.Object && elemVar == null) {
                if (elemVar == null) {
                    WriteUtils.AppendNull(writer);
                } else {
                    var fieldType = field.fieldType;
                    fieldType.WriteObject(writer, elemVar);
                }
            }
            JsonWriter.InstancePop(writer);
            bytes.AppendChar('}');
            WriteUtils.DecLevel(writer, startLevel);
        }
            
        public override T Read(JsonReader reader, T slot, out bool success) {
            // Ensure preconditions are fulfilled
            if (!ObjectUtils.StartObject(reader, this, out success))
                return default;
                
            ref var parser = ref reader.parser;
            T obj = slot;
            TypeMapper classType = this;
            JsonEvent ev = parser.NextEvent();
            if (obj == null) {
                // Is first member is discriminator - "$type": "<typeName>" ?
                if (ev == JsonEvent.ValueString && reader.discriminator.IsEqualBytes(ref parser.key)) {
                    classType = reader.typeCache.GetTypeByName(ref parser.value);
                    if (classType == null)
                        return ReadUtils.ErrorMsg<T>(reader, "Object with discriminator $type not found: ", ref parser.value, out success);
                    ev = parser.NextEvent();
                }
                obj = (T)classType.CreateInstance();
            }

            var payload = JsonReader.InstanceLoad(reader, classType);

            while (true) {
                object elemVar;
                switch (ev) {
                    case JsonEvent.ValueString:
                        PropField field = classType.GetField(ref parser.key);
                        if (field == null) {
                            if (!reader.discriminator.IsEqualBytes(ref parser.key)) // dont count discriminators
                                parser.SkipEvent();
                            break;
                        }
                        var fieldType = field.fieldType;
                        if (reader.useIL && field.isValueType) {
                            if (!fieldType.ReadField(reader, ref payload, field))
                                return default;
                            continue;
                        }
                        elemVar = fieldType.ReadObject(reader, null, out success);
                        if (!success)
                            return default;
                        field.SetField(obj, elemVar); // set also to null in error case
                        break;
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        // todo: check in EncodeJsonToComplex, why listObj[0].i64 & subType.i64 are skipped
                        if ((field = GetField(reader, classType)) == null)
                            break;
                        fieldType = field.fieldType;
                        if (reader.useIL && field.isValueType) {
                            if (!fieldType.ReadField(reader, ref payload, field))
                                return default;
                            continue;
                        }
                        elemVar = fieldType.ReadObject(reader, null, out success);
                        if (!success)
                            return default;
                        field.SetField(obj, elemVar); // set also to null in error case
                        break;
                    case JsonEvent.ValueNull:
                        if ((field = GetField(reader, classType)) == null)
                            break;
                        if (!field.fieldType.isNullable) {
                            ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, field.fieldType, ref parser, out success);
                            return default;
                        }
                        field.SetField(obj, null);
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        if ((field = GetField(reader, classType)) == null)
                            break;
                        elemVar = field.GetField(obj);
                        // if (field.fieldType.varType != VarType.Object) {
                        //     ReadUtils.ErrorMsg<T>(reader, "Expect field of type object. Type: ", field.fieldType.GetNativeType().ToString(), out success);
                        //     return default;
                        // }
                        object sub = elemVar;
                        fieldType = field.fieldType;
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
                        JsonReader.InstanceStore(reader, obj);
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

        private static PropField GetField(JsonReader reader, TypeMapper classType) {
            PropField field = classType.GetField(ref reader.parser.key);
            if (field != null)
                return field;
            reader.parser.SkipEvent();
            return null;
        }
    }
}