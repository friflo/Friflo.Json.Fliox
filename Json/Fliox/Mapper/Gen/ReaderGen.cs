// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;
using Friflo.Json.Fliox.Mapper.Map.Val;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Mapper.Map
{
    delegate bool ReadFieldDelegate<in T>(T obj, PropField field, ref Reader reader);

    partial struct Reader
    {
        private TVal HandleEventGen<TVal>(TypeMapper mapper, out bool success) {
            switch (parser.Event) {
                case JsonEvent.ValueNull:
                    if (!mapper.isNullable) {
                        return ErrorIncompatible<TVal>(mapper.DataTypeName(), mapper, out success);
                    }
                    success = true;
                    return default;
                case JsonEvent.Error:
                    const string msg2 = "requirement: error must be handled by owner. Add missing JsonEvent.Error case to its Mapper";
                    throw new InvalidOperationException(msg2);
                // return null;
                default: {
                    return ErrorIncompatible<TVal>(mapper.DataTypeName(), mapper, out success);
                }
            }
        }

        // ---------------------------------- object - class / struct  ----------------------------------
        public T ReadClass<T> (PropField field, T value, out bool success) where T : class {
            if (parser.Event == JsonEvent.ValueNull) {
                success = true;
                return null;
            }
            if (value == null) {
                // instance creation should be done inside Read<>()
                value = (T)field.fieldType.CreateInstance();
            }
            var mapper = (TypeMapper<T>)field.fieldType;
            return mapper.Read(ref this, value, out success);
        }
        
        /// <summary> <paramref name="value"/> is only used to infer type to avoid setting generic Type T explicit </summary>
        public T ReadStruct<T> (PropField field, in T value, out bool success) where T : struct {
            var mapper = (TypeMapper<T>)field.fieldType;
            if (parser.Event == JsonEvent.ValueNull) {
                return ErrorIncompatible<T>(mapper.DataTypeName(), mapper, out success);
            }
            return mapper.Read(ref this, default, out success);
        }
        
        /// <summary> <paramref name="value"/> is only used to infer type to avoid setting generic Type T explicit </summary>
        public T? ReadStructNull<T> (PropField field, in T? value, out bool success) where T : struct {
            var mapper = (TypeMapper<T>)field.fieldType;
            if (parser.Event == JsonEvent.ValueNull) {
                if (!mapper.isNullable) {
                    return ErrorIncompatible<T>(mapper.DataTypeName(), mapper, out success);
                }
                success = true;
                return null;
            }
            return mapper.Read(ref this, default, out success);
        }
        
        // ------------------------------------------- bool ---------------------------------------------
        public bool ReadBoolean (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueBool)
                return HandleEventGen<bool>(field.fieldType, out success);
            return parser.ValueAsBool(out success);
        }
        
        // --- nullable
        public bool? ReadBooleanNull (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueBool)
                return HandleEventGen<bool?>(field.fieldType, out success);
            return parser.ValueAsBool(out success);
        }
        
        // ------------------------------------------- number ---------------------------------------------
        // --- integer
        public byte ReadByte (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<byte>(field.fieldType, out success);
            return parser.ValueAsByte(out success);
        }
        
        public short ReadInt16 (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<short>(field.fieldType, out success);
            return parser.ValueAsShort(out success);
        }
        
        public int ReadInt32 (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<int>(field.fieldType, out success);
            return parser.ValueAsInt(out success);
        }
        
        public long ReadInt64 (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<long>(field.fieldType, out success);
            return parser.ValueAsLong(out success);
        }
        
        // --- floating point ---
        public float ReadSingle (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<float>(field.fieldType, out success);
            return parser.ValueAsFloat(out success);
        }
        
        public double ReadDouble (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<double>(field.fieldType, out success);
            return parser.ValueAsDouble(out success);
        }
        
        // --------------------------------- nullable number ------------------------------------------
        // --- integer
        public byte? ReadByteNull (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<byte?>(field.fieldType, out success);
            return parser.ValueAsByte(out success);
        }
        
        public short? ReadInt16Null (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<short?>(field.fieldType, out success);
            return parser.ValueAsShort(out success);
        }
        
        public int? ReadInt32Null (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<int?>(field.fieldType, out success);
            return parser.ValueAsInt(out success);
        }
        
        public long? ReadInt64Null (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<long?>(field.fieldType, out success);
            return parser.ValueAsLong(out success);
        }
        
        // --- floating point ---
        public float? ReadSingleNull (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<float?>(field.fieldType, out success);
            return parser.ValueAsFloat(out success);
        }
        
        public double? ReadDoubleNull (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueNumber)
                return HandleEventGen<double?>(field.fieldType, out success);
            return parser.ValueAsDouble(out success);
        }
        
        // ------------------------------------------- string ---------------------------------------------
        public string ReadString (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueString)
                return HandleEventGen<string>(field.fieldType, out success);
            success = true;
            return parser.value.GetString(ref charBuf);
        }
        
        // --- JsonKey
        /// <see cref="JsonKeyMapper.Read"/>
        public JsonKey ReadJsonKey (PropField field, out bool success) {
            if (parser.Event != JsonEvent.ValueString)
                return HandleEventGen<JsonKey>(field.fieldType, out success);
            success = true;
            return new JsonKey(ref parser.value, ref parser.valueParser);
        }
        
        // --- JsonValue
        /// <see cref="JsonValueMapper.Read"/>
        public JsonValue ReadJsonValue (PropField field, out bool success) {
            var stub = jsonWriterStub;
            if (stub == null)
                jsonWriterStub = stub = new Utf8JsonWriterStub();
            
            ref var serializer = ref stub.jsonWriter;
            serializer.InitSerializer();
            serializer.WriteTree(ref parser);
            var json    = serializer.json.AsArray();
            success     = true;
            return new JsonValue (json);
        }
        
        // ------------------------------------------- enum ---------------------------------------------
        /// <summary> <paramref name="value"/> is only used to infer type to avoid setting generic Type T explicit </summary>
        public T ReadEnum<T> (PropField field, T value, out bool success) where T : struct {
            var mapper = (EnumMapper<T>)field.fieldType;
            return mapper.Read(ref this, default, out success);
        }
        
        /// <summary> <paramref name="value"/> is only used to infer type to avoid setting generic Type T explicit </summary>
        public T? ReadEnumNull<T> (PropField field, T? value, out bool success) where T : struct {
            var mapper = (EnumMapperNull<T>)field.fieldType;
            return mapper.Read(ref this, default, out success);
        }
    }
}