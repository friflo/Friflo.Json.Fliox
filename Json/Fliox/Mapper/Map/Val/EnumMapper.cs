// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Utils;

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
        private     readonly    Dictionary<Bytes, T>        stringToEnum;
        private     readonly    Dictionary<T, Bytes>        enumToString;
        //
        internal    readonly    Dictionary<string, string>  stringToDoc;
        private     readonly    EnumConvert<T>              convert;
        
        internal EnumMapperInternal (TypeMapper mapper, StoreConfig config) {
            this.mapper         = mapper;
            convert             = EnumConvert.GetEnumConvert<T>();
            Type enumType       = mapper.isNullable ? mapper.nullableUnderlyingType : mapper.type;
            FieldInfo[] fields  = enumType.GetFields();
            var count           = fields.Length;
            stringToEnum        = new Dictionary<Bytes, T>(count, Bytes.Equality);
            enumToString        = new Dictionary<T, Bytes>(count);
            var enumContext     = new EnumContext(enumType, config.assemblyDocs);
            var names           = CreateNames(fields);
            for (int n = 0; n < count; n++) {
                FieldInfo enumField = fields[n];
                if (!enumField.FieldType.IsEnum)
                    continue;
                T       enumValue   = (T)enumField.GetValue(enumType);
                string  enumName    = enumField.Name;
                Bytes   name        = names[n].AsBytes();
                name.UpdateHashCode();
                stringToEnum.Add   (name, enumValue);
                enumToString.TryAdd(enumValue, name);
                enumContext.AddEnumValueDoc(ref stringToDoc, enumName);
            }
        }
        
        /// Add enum values to buffer in a separate loop to use only a single <see cref="Utf8Buffer"/> buffer array
        private static Utf8String[] CreateNames(FieldInfo[] fields) {
            var buffer  = new Utf8Buffer();
            var names   = new Utf8String[fields.Length];
            for (int n = 0; n < fields.Length; n++) {
                FieldInfo enumField = fields[n];
                if (!enumField.FieldType.IsEnum)
                    continue;
                names[n]    = buffer.Add(enumField.Name);
            }
            var remainder = new string('-', Bytes.CopyRemainder);
            buffer.Add(remainder);
            return names;
        }
        
        internal  List<string>    GetEnumValues() {
            var enumValues = new List<string>();
            foreach (var pair in stringToEnum) {
                var     enumValueBytes  = pair.Key;
                string  enumValue       = enumValueBytes.ToString();
                enumValues.Add(enumValue);
            }
            return enumValues;
        }
        
        internal void Write(ref Writer writer, T slot) {
            if (enumToString.TryGetValue(slot, out var enumName)) {
                writer.bytes.AppendChar('\"');
                writer.bytes.AppendBytes(ref enumName);
                writer.bytes.AppendChar('\"');
            }
        }
        
        internal T Read(ref Reader reader, T slot, out bool success) {
            ref var parser = ref reader.parser;
            if (parser.Event == JsonEvent.ValueString) {
                reader.parser.value.UpdateHashCode();
                if (stringToEnum.TryGetValue(reader.parser.value, out T enumValue)) {
                    success = true;
                    return enumValue;
                }
                return reader.ErrorIncompatible<T>("enum ", typeof(T).Name, mapper, out success);
            }
            if (parser.Event == JsonEvent.ValueNumber) {
                int integralValue = parser.ValueAsInt(out success);
                if (!success)
                    return default;
                var enumValue = convert.IntToEnum(integralValue);
                if (enumToString.ContainsKey(enumValue)) {
                    success = true;
                    return enumValue;
                }
                return reader.ErrorIncompatible<T>("enum ", typeof(T).Name, mapper, out success);
            }
            return reader.ErrorIncompatible<T>(mapper.DataTypeName(), mapper, out success);
        }
    }

    internal sealed class EnumMapper<T> : TypeMapper<T> where T : struct
    {
        private readonly    EnumMapperInternal<T>               intern;

        public override     string                              DataTypeName()      => $"enum {typeof(T).Name}";
        public override     bool                                IsNull(ref T value) => false;
        public override     List<string>                        GetEnumValues()     => intern.GetEnumValues();
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
            return intern.Read(ref reader, slot, out success);
        }        
    }
    
    internal sealed class EnumMapperNull<T> : TypeMapper<T?> where T : struct
    {
        private readonly    EnumMapperInternal<T>               intern;

        public override     string                              DataTypeName()          =>  $"enum {typeof(T).Name}?";
        public override     bool                                IsNull(ref T? value)    => !value.HasValue;
        public override     List<string>                        GetEnumValues()         => intern.GetEnumValues();
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
            return intern.Read(ref reader, default, out success);
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
