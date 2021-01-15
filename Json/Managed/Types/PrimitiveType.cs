// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Codecs;

namespace Friflo.Json.Managed.Types
{
    public class PrimitiveType : StubType
    {
        
        public PrimitiveType(Type type, IJsonCodec codec)
            : base(type, codec, IsPrimitiveNullable(type), GetTypeCat(type))
        {
        }

        public override void InitStubType(TypeStore typeStore) {
        }

        private static bool IsPrimitiveNullable(Type type) {
            return Nullable.GetUnderlyingType(type) != null;
        }
        
        private static TypeCat GetTypeCat(Type type) {
            if (type == typeof(string))
                return TypeCat.String;
            
            if (type == typeof(bool))
                return TypeCat.Bool;
            if (
                type == typeof(long)  || type == typeof(long?)  ||
                type == typeof(int)   || type == typeof(int?)   ||
                type == typeof(short) || type == typeof(short?) ||
                type == typeof(byte)  || type == typeof(byte?)  ||
                type == typeof(bool)  || type == typeof(bool?)  ||
                type == typeof(double)|| type == typeof(double?)||
                type == typeof(float) || type == typeof(float))
                return TypeCat.Number;
            return TypeCat.None;
        }
    }
    
    public class StringType : StubType
    {
        public StringType(Type type, IJsonCodec codec)
            : base(type, codec, true, TypeCat.String) {
        }
        
        public override void InitStubType(TypeStore typeStore) {
        }
    }

}