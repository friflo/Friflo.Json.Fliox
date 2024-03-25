// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Numerics;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.Schema.Validation
{
    public sealed class NativeValidationSet : IDisposable
    {
        private  readonly   ConcurrentDictionary<Type, bool>                clientTypes;
        private  readonly   ConcurrentDictionary<Type, ValidationType>      validationTypes;
        private  readonly   ConcurrentDictionary<Type, ValidationTypeDef>   validationTypeDefs;
        private  readonly   TypeStore                                       typeStore;
        
        public NativeValidationSet() {
            clientTypes         = new ConcurrentDictionary<Type, bool>();
            validationTypes     = new ConcurrentDictionary<Type, ValidationType>();
            validationTypeDefs  = new ConcurrentDictionary<Type, ValidationTypeDef>();
            typeStore           = new TypeStore();
            AddRootType(typeof(StandardTypes));
        }

        public void Dispose() {
            typeStore.Dispose();
        }

        public ValidationType GetValidationType(Type type) {
            if (validationTypes.TryGetValue(type, out var validationType)) {
                return validationType;
            }
            if (validationTypeDefs.TryGetValue(type, out var typeDef)) {
                validationType = typeDef.validationType;
                validationTypes.TryAdd(type, validationType);
                return validationType;
            }
            validationType = GetValidationTypeInternal(type);
            validationTypes.TryAdd(type, validationType);
            return validationType;
        }
        
        private ValidationType  GetValidationTypeInternal(Type type) {
            var mapper          = typeStore.GetTypeMapper(type);
            var isNullable      = mapper.isNullable;
            if (mapper.isNullable && mapper.nullableUnderlyingType != null) {
                mapper = typeStore.GetTypeMapper(mapper.nullableUnderlyingType);
            }
            var isArray             = mapper.IsArray;
            var isDictionary        = mapper.IsDictionary;
            var isNullableElement   = false;
            if (isArray || isDictionary) {
                mapper = mapper.GetElementMapper();
                if (mapper.isNullable && mapper.nullableUnderlyingType != null) {
                    mapper = typeStore.GetTypeMapper(mapper.nullableUnderlyingType);
                    isNullableElement = true;
                }
            }
            var nativeSchema        = NativeTypeSchema.Create(mapper.type);
            var validationSet       = new ValidationSet(nativeSchema);
            var typeDef             = nativeSchema.GetNativeType(mapper.type);
            var validationTypeDef   = validationSet.GetValidationTypeDef(typeDef);
            return new ValidationType(validationTypeDef, isNullable, isArray, isDictionary, isNullableElement);
        }
        
        /// <summary>
        /// <see cref="AddRootType"/> is used for optimization.<br/> 
        /// It create <see cref="ValidationTypeDef"/> instances for the given <paramref name="rootType"/> and all its dependent types.
        /// <br/>
        /// The <paramref name="rootType"/> is typically a class type extending FlioxClient - containing all
        /// application specific types like entity, command and message types <br/>
        /// Adding all <see cref="ValidationTypeDef"/> instances at once enable reduced memory consumption and
        /// high memory locality as only a single <see cref="ValidationSet"/> is created for a single database schema.
        /// </summary>
        public void AddRootType (Type rootType) {
            if (!clientTypes.TryAdd(rootType, true))
                return;
            var nativeSchema    = NativeTypeSchema.Create(rootType);
            var validationSet   = new ValidationSet(nativeSchema);
            foreach (var typeDef in validationSet.TypeDefs) {
                var nativeTypeDef = (NativeTypeDef)typeDef.typeDef;
                validationTypeDefs.TryAdd(nativeTypeDef.native, typeDef);
            }
        }
    }
    
#pragma warning disable CS0649
    internal sealed class StandardTypes
    {
        public  bool        stdBool;
        public  byte        stdByte;
        public  short       stdShort;
        public  int         stdInt;
        public  long        stdLong;
        public  float       stdFloat;
        public  double      stdDouble;
        public  string      stdString;
        public  DateTime    stdDateTime;
        public  Guid        stdGuid;
        public  BigInteger  stdBigInteger;
        public  JsonValue   stdJsonValue;
    }
}