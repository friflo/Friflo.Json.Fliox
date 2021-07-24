// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Schema.Utils
{
    public interface ITypeSystem
    {
        ICollection<ITyp> Types { get;}
        
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
        
        ITyp   JsonValue   { get; }
        
        ICollection<ITyp> GetTypes(ICollection<Type> separateTypes);
    }
    
    public class NativeTypeSystem : ITypeSystem
    {
        private  readonly   ICollection<ITyp> types;
        
        private  readonly   NativeType  boolean;
        private  readonly   NativeType  @string;
        private  readonly   NativeType  uint8;
        private  readonly   NativeType  int16;
        private  readonly   NativeType  int32;
        private  readonly   NativeType  int64;
        private  readonly   NativeType  flt32;
        private  readonly   NativeType  flt64;
        private  readonly   NativeType  bigInteger;
        private  readonly   NativeType  dateTime;
        private  readonly   NativeType  jsonValue;

        public              ICollection<ITyp>               Types => types;
        private  readonly   Dictionary<Type, NativeType>    nativeMap;
        
        public              ITyp    Boolean    => boolean;
        public              ITyp    String     => @string;
        public              ITyp    Unit8      => uint8;
        public              ITyp    Int16      => int16;
        public              ITyp    Int32      => int32;
        public              ITyp    Int64      => int64;
        public              ITyp    Float      => flt32;
        public              ITyp    Double     => flt64;
        public              ITyp    BigInteger => bigInteger;
        public              ITyp    DateTime   => dateTime;
        public              ITyp    JsonValue  => jsonValue;
        
        public NativeTypeSystem (IReadOnlyDictionary<Type, TypeMapper> typeMappers) {
            nativeMap   = new Dictionary<Type, NativeType>(typeMappers.Count);
            var map     = new Dictionary<ITyp, TypeMapper>(typeMappers.Count);
            foreach (var pair in typeMappers) {
                var type    = pair.Key; 
                var mapper  = pair.Value;
                var iTyp = new NativeType(mapper);
                map.      Add(iTyp, mapper);
                nativeMap.Add(type, iTyp);
            }
            this.types = map.Keys;
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
            jsonValue   = nativeMap[typeof(JsonValue)];
            foreach (var pair in nativeMap) {
                var type  = pair.Value;
                type.baseType = nativeMap[type.native].BaseType;
            }
            foreach (var pair in map) {
                var type    = pair.Key;
                var mapper  = pair.Value;
                type.ElementType = nativeMap[mapper.GetElementMapper().type]; 

                foreach (var propField in mapper.propFields.fields) {
                    var field = new Field {
                        jsonName    = propField.jsonName,
                        required    = propField.required,
                        fieldType   = nativeMap[propField.fieldType.type]
                    };
                    type.Fields.Add(field);
                }                
            }
        }
        
        public ICollection<ITyp> GetTypes(ICollection<Type> nativeTypes) {
            var list = new List<ITyp> (nativeTypes.Count);
            foreach (var nativeType in nativeTypes) {
                var type = nativeMap[nativeType];
                list.Add(type);
            }
            return list;
        }
    }
}