// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Types
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ClassType : StubType
    {
        private readonly Dictionary <string, PropField> strMap      = new Dictionary <string, PropField>(13);
        private readonly HashMapOpen<Bytes,  PropField> fieldMap;
        public  readonly PropertyFields                 propFields;
        private readonly ConstructorInfo                constructor;
        private readonly Bytes                          removedKey;
        
        
        public override void Dispose() {
            base.Dispose();
            propFields.Dispose();
            removedKey.Dispose();
        }

        // PropType
        internal ClassType (Type type, TypeMapper map, ConstructorInfo constructor) :
            base (type, map, IsNullable(type)) {
            removedKey = new Bytes("__REMOVED");
            fieldMap = new HashMapOpen<Bytes, PropField>(11, removedKey);

            propFields = new  PropertyFields (type, this);
            for (int n = 0; n < propFields.num; n++)
            {
                PropField   field = propFields.fields[n];
                if (strMap.ContainsKey(field.name))
                    throw new InvalidOperationException("assert field is accessible via string lookup");
                strMap.Add(field.name, field);
                fieldMap.Put(ref field.nameBytes, field);
            }
            this.constructor = constructor;
        }

        public override void InitStubType(TypeStore typeStore) {
            for (int n = 0; n < propFields.num; n++) {
                PropField field = propFields.fields[n];

                StubType stubType = typeStore.GetType(field.fieldTypeNative);
                FieldInfo fieldInfo = field.GetType().GetField("fieldType");
                // ReSharper disable once PossibleNullReferenceException
                fieldInfo.SetValue(field, stubType);
                field.collectionConstructor  = field.fieldType is CollectionType propCollection ? propCollection.constructor : null;
            }
        }
        
        private static bool IsNullable(Type type) {
            return !type.IsValueType;
        }
        
        public override Object CreateInstance()
        {
            if (constructor == null) {
                // Is it a struct?
                if (type.IsValueType)
                    return Activator.CreateInstance(type);
                throw new FrifloException("No default constructor available for: " + type.Name);
            }
            return Reflect.CreateInstance(constructor);
        }

        public PropField GetField (ref Bytes fieldName) {
            // Note: its likely that hashcode ist not set properly. So calculate anyway
            fieldName.UpdateHashCode();
            PropField pf = fieldMap.Get(ref fieldName);
            if (pf == null)
                Console.Write("");
            return pf;
        }
        

    }   
}
