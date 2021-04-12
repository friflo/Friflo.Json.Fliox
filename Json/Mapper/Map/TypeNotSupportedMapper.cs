// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Flow.Mapper.Map
{
    public class TypeNotSupportedMatcher : ITypeMatcher {
        public static readonly TypeNotSupportedMatcher Instance = new TypeNotSupportedMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            return CreateTypeNotSupported(config, type, "");
        }

        public static TypeMapper CreateTypeNotSupported(StoreConfig config, Type type, string msg) {
            //  new TypeNotSupportedMapper (config, type, "Type not supported. Type: " + type);
            object[] constructorParams = {config, type, $"Type not supported. {msg} Type: " + type};
            var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(TypeNotSupportedMapper<>), new[] {type}, constructorParams);
            return (TypeMapper) newInstance;
        }
    }


#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class TypeNotSupportedMapper<T> : TypeMapper<T>
    {
        private readonly string msg;
        
        public override string DataTypeName() { return "unsupported type"; }

        public TypeNotSupportedMapper(StoreConfig config, Type type, string msg) : base(config, type, true, false) {
            this.msg = msg;
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            throw new NotSupportedException(msg);
        }

        public override void Write(ref Writer writer, T slot) {
            throw new NotSupportedException(msg);
        }
    }
}