// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using System.Numerics;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public sealed class BinaryDbDataReader
    {
        private     int                         currentOrdinal;
        private     DbDataReader                reader;
        private     ObjectPool<ObjectMapper>    objectMapper;
        
        public BinaryDbDataReader(ObjectPool<ObjectMapper> objectMapper) {
            this.objectMapper = objectMapper;
        }
       
        public void Init(DbDataReader reader) {
            this.reader = reader;
            currentOrdinal = 0;
        }
        
        public object Read(TypeMapper mapper, object obj) {
            currentOrdinal = 0;
            return ReadIntern(mapper, obj);
        }
        
        private object ReadIntern(TypeMapper mapper, object obj)
        {
            obj         ??= mapper.NewInstance();
            var fields    = mapper.PropFields.fields;
            foreach (var field in fields)
            {
                if (field.typeId != StandardTypeId.Object) {
                    Var memberVal = GetVar(field);
                    field.member.SetVar(obj, memberVal);
                } else {
                    // typeId == StandardTypeId.Object
                    var fieldType = field.fieldType;
                    if (!HasObject(field.fieldType)) {
                        field.member.SetVar(obj, new Var((object)null));
                        continue;
                    }
                    Var memberObjVar    = field.member.GetVar(obj);
                    var memberObjCur    = memberObjVar.Object;
                    var memberObj       = ReadIntern(fieldType, memberObjCur);
                    if (ReferenceEquals(memberObjCur, memberObj)) {
                        continue;
                    }
                    field.member.SetVar(obj, new Var(memberObj));
                }
            }
            return obj;
        }
        
        private bool HasObject (TypeMapper mapper) {
            int ordinal = currentOrdinal++;
            bool hasObject = !reader.IsDBNull(ordinal) && reader.GetByte(ordinal) != 0;
            if (hasObject) {
                return true;
            }
            var propFields = mapper.PropFields;
            if (propFields != null) {
                currentOrdinal += propFields.fields.Length;
            }
            return false;
        }
        
        private Var GetVar(PropField field)
        {
            int ordinal = currentOrdinal++;
            /* if (reader.IsDBNull(ordinal)) {
                return new Var(field.fieldType.varType.DefaultValue);
            } */
            if (!field.isNullable)
            {
                switch (field.typeId)
                {
                    case StandardTypeId.Boolean:    return new Var(reader.GetBoolean    (ordinal));
                    case StandardTypeId.String:     return GetString(reader.GetString   (ordinal), field);
                    //
                    case StandardTypeId.Uint8:      return new Var(reader.GetByte       (ordinal));
                    case StandardTypeId.Int16:      return new Var(reader.GetInt16      (ordinal));
                    case StandardTypeId.Int32:      return new Var(reader.GetInt32      (ordinal));
                    case StandardTypeId.Int64:      return new Var(reader.GetInt64      (ordinal));
                    //
                    case StandardTypeId.Float:      return new Var(reader.GetFloat      (ordinal));
                    case StandardTypeId.Double:     return new Var(reader.GetDouble     (ordinal));
                    //
                    case StandardTypeId.DateTime:   return new Var(reader.GetDateTime   (ordinal));
                    case StandardTypeId.Guid:       return new Var(reader.GetGuid       (ordinal));
                    //
                    case StandardTypeId.JsonKey:    return new Var(new JsonKey(     reader.GetString(ordinal)));
                    case StandardTypeId.BigInteger: return new Var(BigInteger.Parse(reader.GetString(ordinal)));
                    case StandardTypeId.JsonValue:  return new Var(new JsonValue(   reader.GetString(ordinal)));
                    case StandardTypeId.Array:      return new Var(GetArray(field,                   ordinal));
                }
                throw new NotImplementedException($"GetVar() {field.typeId}");
            }
            var value = reader.GetValue(ordinal);
            if (value == DBNull.Value) {
                return field.fieldType.varType.DefaultValue;
            }
            switch (field.typeId)
            {
                case StandardTypeId.Boolean:    return new Var((bool?)      value);
                case StandardTypeId.String:     return GetString((string)   value, field);
                //
                case StandardTypeId.Uint8:      return new Var((byte?)      value);
                case StandardTypeId.Int16:      return new Var((short?)     value);
                case StandardTypeId.Int32:      return new Var((int?)       value);
                case StandardTypeId.Int64:      return new Var((long?)      value);
                //
                case StandardTypeId.Float:      return new Var((float?)     value);
                case StandardTypeId.Double:     return new Var((double?)    value);
                //
                case StandardTypeId.DateTime:   return new Var((DateTime?)  value);
                case StandardTypeId.Guid:       return new Var((Guid?)      value);
                //
                case StandardTypeId.JsonKey:    return new Var(new JsonKey(     reader.GetString(ordinal)));
                case StandardTypeId.BigInteger: return new Var(BigInteger.Parse(reader.GetString(ordinal)));
                case StandardTypeId.JsonValue:  return new Var(new JsonValue(   reader.GetString(ordinal)));
                case StandardTypeId.Array:      return new Var(GetArray(field,                   ordinal));
            }
            throw new NotImplementedException($"GetVar() {field.typeId}");
        }
        
        private static Var GetString(string value, PropField field) {
            if (field.fieldType.type == typeof(string)) {
                return new Var(value);
            }
            return new Var(new ShortString(value));
        }
        
        private object GetArray(PropField field, int ordinal) {
            var jsonArray = reader.GetString(ordinal);
            if (jsonArray == null) {
                return null;
            }
            using var pooled = objectMapper.Get();  // TODO cache mapper to avoid .Get()
            var obj = pooled.instance.ReadObject(jsonArray, field.fieldType.type);
            return obj;
        }
    }
}