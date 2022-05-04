// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.Schema.Validation
{
    public class NativeValidationSet
    {
        private readonly    HashSet<Type>                       clientTypes;
        private readonly    Dictionary<Type, ValidationType>    validationTypes;
        private readonly    Dictionary<Type, ValidationTypeDef> validationTypeDefs;
        
        public NativeValidationSet() {
            clientTypes         = new HashSet<Type>();
            validationTypes     = new Dictionary<Type, ValidationType>();
            validationTypeDefs  = new Dictionary<Type, ValidationTypeDef>();
            AddRootType(typeof(StandardTypes));
        }
        
        public ValidationType GetValidationType(Type type) {
            if (validationTypes.TryGetValue(type, out var validationType)) {
                return validationType;
            }
            if (validationTypeDefs.TryGetValue(type, out var typeDef)) {
                validationType = typeDef.validationType;
                validationTypes.Add(type, validationType);
                return validationType;
            }
            validationType = GetValidationTypeInternal(type);
            validationTypes.Add(type, validationType);
            return validationType;
        }
        
        private static ValidationType  GetValidationTypeInternal(Type type) {
            var nativeSchema    = NativeTypeSchema.Create(type);
            var validationSet   = new ValidationSet(nativeSchema);
            var attr            = nativeSchema.GetArgAttributes(type);
            var typeDef         = attr.typeDef;
            var fieldDef        = new FieldDef("param", attr.required, false, false, typeDef, attr.isArray, attr.isDictionary, false, null, null, null, nativeSchema.Utf8Buffer);
            var validationType  = new ValidationType(fieldDef, -1);
            if (attr.isArray || attr.isDictionary) {
                var elementMapper       = typeDef.mapper.GetElementMapper();
                var elementTypeDef      = nativeSchema.GetArgAttributes(elementMapper.type);
                validationType.typeDef  = validationSet.GetValidationTypeDef(elementTypeDef.typeDef);
            } else {
                validationType.typeDef  = validationSet.GetValidationTypeDef(typeDef);
            }
            return validationType;
        }
        
        public void AddRootType (Type rootType) {
            return;
            if (!clientTypes.Add(rootType))
                return;
            var nativeSchema    = NativeTypeSchema.Create(rootType);
            var validationSet   = new ValidationSet(nativeSchema);
            foreach (var typeDef in validationSet.TypeDefs) {
                var nativeTypeDef = (NativeTypeDef)typeDef.typeDef;
                validationTypeDefs.TryAdd(nativeTypeDef.native, typeDef);
            }
        }
        
#pragma warning disable CS0649
        private class StandardTypes
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
        }
    }
}