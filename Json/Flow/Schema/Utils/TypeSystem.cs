// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema.Utils
{
    public interface ITypeSystem
    {
        IReadOnlyDictionary<ITyp, TypeMapper> TypeMappers { get;}
        
        ITyp   Boolean     { get; }
        ITyp   String      { get; }
        
        ITyp   Unit8       { get; }
        ITyp   Int16       { get; }
        ITyp   Int32       { get; }
        ITyp   Int64       { get; }
        
        ITyp   Float       { get; }
        ITyp   Double      { get; }
        
        ITyp   BigInteger  { get; }
        ITyp   DateTime    { get; }
    }
    
    public class NativeTypeSystem : ITypeSystem
    {
        private readonly IReadOnlyDictionary<ITyp, TypeMapper> typeMappers;
        
        private readonly NativeType boolean;
        private readonly NativeType @string;
        private readonly NativeType uint8;
        private readonly NativeType int16;
        private readonly NativeType int32;
        private readonly NativeType int64;
        private readonly NativeType flt32;
        private readonly NativeType flt64;
        private readonly NativeType bigInteger;
        private readonly NativeType dateTime;

        public IReadOnlyDictionary<ITyp, TypeMapper> TypeMappers => typeMappers;
        public ITyp Boolean    => boolean;
        public ITyp String     => @string;
        public ITyp Unit8      => uint8;
        public ITyp Int16      => int16;
        public ITyp Int32      => int32;
        public ITyp Int64      => int64;
        public ITyp Float      => flt32;
        public ITyp Double     => flt64;
        public ITyp BigInteger => bigInteger;
        public ITyp DateTime   => dateTime;
        
        public NativeTypeSystem (IReadOnlyDictionary<Type, TypeMapper> typeMappers) {
            var nativeMap   = new Dictionary<Type, NativeType>(typeMappers.Count);
            var map         = new Dictionary<ITyp, TypeMapper>(typeMappers.Count);
            foreach (var pair in typeMappers) {
                var type    = pair.Key; 
                var mapper  = pair.Value;
                var iTyp = new NativeType(type);
                map.      Add(iTyp, mapper);
                nativeMap.Add(type, iTyp);
            }
            this.typeMappers = new ReadOnlyDictionary<ITyp, TypeMapper>(map);
            boolean     = nativeMap[typeof(bool)];
            @string     = nativeMap[typeof(string)];
            uint8       = nativeMap[typeof(byte)];
            int16       = nativeMap[typeof(short)];
            int32       = nativeMap[typeof(int)];
            int64       = nativeMap[typeof(long)];
            flt32       = nativeMap[typeof(float)];
            flt64       = nativeMap[typeof(double)];
            bigInteger  = nativeMap[typeof(BigInteger)];
            dateTime    = nativeMap[typeof(DateTime)];
            foreach (var pair in nativeMap) {
                var type  = pair.Value;
                type.baseType = nativeMap[type.native].BaseType;
            }
        }
    }
}