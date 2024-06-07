// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    internal sealed class EnumMatcher : ITypeMatcher {
        public static readonly EnumMatcher Instance = new EnumMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (!IsEnum(type, out bool _))
                return null;
            object[] constructorParams = {config, type};
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null) {
                // new EnumMapperNull<T> (config, type)
                var enumNullMapper = TypeMapperUtils.CreateGenericInstance(typeof(EnumMapperNull<>), new[] {underlyingType}, constructorParams);
                return (TypeMapper)enumNullMapper;
            }
            // new EnumMapper<T> (config, type)
            var enumMapper = TypeMapperUtils.CreateGenericInstance(typeof(EnumMapper<>), new[] {type}, constructorParams);
            return (TypeMapper)enumMapper;
        }
        
        public static bool IsEnum(Type type, out bool isNullable) {
            isNullable = false;
            if (!type.IsEnum) {
                Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof( Nullable<>) );
                if (args == null)
                    return false;
                Type nullableType = args[0];
                if (!nullableType.IsEnum)
                    return false;
                isNullable = true;
            }
            return true;
        }
    }
    
    /// <summary>
    /// The mapping <see cref="enumToString"/> and <see cref="stringToEnum"/> is not bidirectional as this is the behaviour of C# enum types
    /// <code>
    /// public enum TestEnum {
    ///     Value1 = 11,
    ///     Value2 = 11, // duplicate constant value - C#/.NET maps these enum values to the first value using same constant
    /// }
    /// </code>
    /// </summary>  
    internal sealed class EnumMapperInternal<T> where T : struct
    {
        private     readonly    TypeMapper                  mapper;
        private     readonly    Dictionary<BytesHash, T>    stringToEnum;
        private     readonly    Dictionary<T, Bytes>        enumToString;
        private     readonly    EnumInfo<T>[]               enumInfos;
        //
        internal    readonly    Dictionary<string, string>  stringToDoc;
        private     readonly    EnumConvert<T>              convert;
        
        internal EnumMapperInternal (TypeMapper mapper, StoreConfig config) {
            this.mapper         = mapper;
            convert             = EnumConvert.GetEnumConvert<T>();
            Type enumType       = mapper.isNullable ? mapper.nullableUnderlyingType : mapper.type;
            FieldInfo[] fields  = enumType.GetFields();
            enumInfos           = CreateEnumInfos(fields, enumType);
            var count           = enumInfos.Length;
            stringToEnum        = new Dictionary<BytesHash, T>(count, BytesHash.Equality);
            enumToString        = new Dictionary<T, Bytes>(count);
            var enumContext     = new EnumContext(enumType, config.assemblyDocs);
            var names           = CreateNames(enumInfos);
            for (int n = 0; n < count; n++) {
                var enumInfo    = enumInfos[n];
                var name        = names[n].AsBytes();
                var key         = new BytesHash(name);
                stringToEnum.Add   (key, enumInfo.value);
                enumToString.TryAdd(enumInfo.value, name);
                enumContext.AddEnumValueDoc(ref stringToDoc, enumInfo.name);
            }
        }
        
        private static EnumInfo<T>[] CreateEnumInfos(FieldInfo[] fields, Type enumType) {
            var result = new List<EnumInfo<T>>(fields.Length);
            foreach (var enumField in fields) {
                if (!enumField.FieldType.IsEnum)
                    continue;
                var enumValue   = (T)enumField.GetValue(enumType);
                result.Add(new EnumInfo<T>(enumField.Name, enumValue));
            }
            return result.ToArray();
        }
        
        /// Add enum values to buffer in a separate loop to use only a single <see cref="Utf8Buffer"/> buffer array
        private static Utf8String[] CreateNames(EnumInfo<T>[] enumValues) {
            var buffer  = new Utf8Buffer();
            var names   = new Utf8String[enumValues.Length];
            for (int n = 0; n < enumValues.Length; n++) {
                names[n]    = buffer.Add(enumValues[n].name);
            }
            var remainder = new string('-', Bytes.CopyRemainder);
            buffer.Add(remainder);
            return names;
        }
        
        internal  IReadOnlyList<string>    GetEnumValues() {
            var result = new string[enumInfos.Length];
            for (int n = 0; n < enumInfos.Length; n++) {
                result[n] = enumInfos[n].name;
            }
            return result;
        }
        
        internal void Write(ref Writer writer, T slot) {
            if (enumToString.TryGetValue(slot, out var enumName)) {
                writer.bytes.AppendChar('\"');
                writer.bytes.AppendBytes(enumName);
                writer.bytes.AppendChar('\"');
            }
        }
        
        internal T Read(ref Reader reader, out bool success) {
            ref var parser = ref reader.parser;
            if (parser.Event == JsonEvent.ValueString) {
                var key = new BytesHash(reader.parser.value);
                if (stringToEnum.TryGetValue(key, out T enumValue)) {
                    success = true;
                    return enumValue;
                }
                return reader.ErrorIncompatible<T>("enum ", typeof(T).Name, mapper, out success);
            }
            if (parser.Event == JsonEvent.ValueNumber) {
                long integralValue = parser.ValueAsLong(out success);
                if (!success)
                    return default;
                var enumValue = convert.LongToEnum(integralValue);
                if (enumToString.ContainsKey(enumValue)) {
                    success = true;
                    return enumValue;
                }
                return reader.ErrorIncompatible<T>("enum ", typeof(T).Name, mapper, out success);
            }
            return reader.ErrorIncompatible<T>(mapper.DataTypeName(), mapper, out success);
        }
    }
    
    internal readonly struct EnumInfo<T> where T : struct
    {
        internal readonly   string  name;
        internal readonly   T       value; // integral type - commonly int

        public   override   string  ToString() => name;

        internal EnumInfo(string name, T value) {
            this.name   = name;
            this.value  = value;
        }
    }

    internal sealed class EnumMapper<T> : TypeMapper<T> where T : struct
    {
        private readonly    EnumMapperInternal<T>               intern;

        public override     StandardTypeId                      StandardTypeId      => StandardTypeId.Enum;
        public override     string                              DataTypeName()      => $"enum {typeof(T).Name}";
        public override     bool                                IsNull(ref T value) => false;
        public override     IReadOnlyList<string>               GetEnumValues()     => intern.GetEnumValues();
        public override     IReadOnlyDictionary<string, string> GetEnumValueDocs()  => intern.stringToDoc;

        public EnumMapper(StoreConfig config, Type type) :
            base (config, typeof(T), false, true)
        {
            intern          = new EnumMapperInternal<T>(this, config);
        }

        public override void Write(ref Writer writer, T slot) {
            intern.Write(ref writer, slot);
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            return intern.Read(ref reader, out success);
        }        
    }
    
    internal sealed class EnumMapperNull<T> : TypeMapper<T?> where T : struct
    {
        private readonly    EnumMapperInternal<T>               intern;

        public override     StandardTypeId                      StandardTypeId          => StandardTypeId.Enum;
        public override     string                              DataTypeName()          =>  $"enum {typeof(T).Name}?";
        public override     bool                                IsNull(ref T? value)    => !value.HasValue;
        public override     IReadOnlyList<string>               GetEnumValues()         => intern.GetEnumValues();
        public override     IReadOnlyDictionary<string, string> GetEnumValueDocs()      => intern.stringToDoc;

        public EnumMapperNull(StoreConfig config, Type type) :
            base (config, typeof(T?), true, true)
        {
            intern          = new EnumMapperInternal<T>(this, config);
        }

        public override void Write(ref Writer writer, T? slot) {
            if (!slot.HasValue) throw new InvalidOperationException("Expect enum value not null");
            T value = slot.Value;
            intern.Write(ref writer, value);
        }

        public override T? Read(ref Reader reader, T? slot, out bool success) {
            if (reader.parser.Event == JsonEvent.ValueNull) {
                success = true;
                return null;
            }
            return intern.Read(ref reader, out success);
        }
    }
    
    internal readonly struct EnumContext
    {
        private     readonly    Assembly        assembly;
        private     readonly    AssemblyDocs    assemblyDocs;
        private     readonly    string          @namespace;
        
        internal EnumContext(Type enumType, AssemblyDocs assemblyDocs) {
            assembly            = enumType.Assembly;
            this.assemblyDocs   = assemblyDocs;
            @namespace          = enumType.FullName;
        }
        
        public void AddEnumValueDoc(ref Dictionary<string, string> stringToDoc, string enumName) {
            if (assembly == null || assemblyDocs == null)
                return;
            var signature           = $"F:{@namespace}.{enumName}";
            var valueDoc            = assemblyDocs.GetDocs(assembly, signature);
            if (valueDoc == null)
                return;
            if (stringToDoc == null) {
                stringToDoc = new Dictionary<string, string>();
            }
            stringToDoc.Add(enumName, valueDoc);
        }
    }
}
