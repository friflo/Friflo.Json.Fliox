// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Obj.Reflect
{
    public abstract class PropertyFields {
        public   readonly   PropField []                    fields;
        public   readonly   Bytes32 []                      names32;
        public   readonly   int                             Count;
        public   readonly   int                             primCount;
        public   readonly   int                             objCount;
        // ReSharper disable once NotAccessedField.Local
        private  readonly   string                          typeName;
        
        public   abstract   PropField                       GetPropField (string fieldName);
        
        protected PropertyFields(FieldQuery query) {
            typeName        = query.type. ToString();
            primCount       = query.primCount;
            objCount        = query.objCount;
            Count           = query.fields.Count;
            fields          = query.fields.ToArray();
            names32         = new Bytes32[Count];
        }
    }
    
    // PropertyFields
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class PropertyFields<T> : PropertyFields, IDisposable
    {
        public   readonly   PropField<T> []                     typedFields;
        private  readonly   Dictionary <string, PropField<T>>   strMap      = new Dictionary <string, PropField<T>>(13);
        private  readonly   HashMapOpen<Bytes,  PropField<T>>   fieldMap;
        
        private  readonly   Bytes                               removedKey;
        
        public   override   PropField                           GetPropField (string fieldName) => GetField(fieldName);
        
        public PropertyFields (FieldQuery<T> query)
            : base (query)
        {

            var fieldList   = query.fieldList;
            removedKey      = new Bytes("__REMOVED", Untracked.Bytes);
            fieldMap        = new HashMapOpen<Bytes, PropField<T>>(11, removedKey);
            
            typedFields     = new PropField<T> [Count];
            
            for (int n = 0; n < Count; n++) {
                typedFields[n]  = fieldList[n];
                var field       = typedFields[n];
                if (strMap.ContainsKey(field.name))
                    throw new InvalidOperationException("assert field is accessible via string lookup");
                strMap.Add(field.name, field);
                fieldMap.Put(ref field.nameBytes, field);
                names32[n].FromBytes(ref field.nameBytes);
            }
            fieldList. Clear();
        }
        
        public bool Contains(string fieldName) {
            return strMap.ContainsKey(fieldName);
        }
        
        public PropField<T> GetField (ref Bytes fieldName) {
            // Note: its likely that hashcode ist not set properly. So calculate anyway
            fieldName.UpdateHashCode();
            PropField<T> pf = fieldMap.Get(ref fieldName);
            return pf;
        }
        
        public PropField<T> GetField (string fieldName) {
            strMap.TryGetValue(fieldName, out PropField<T> field);
            return field;
        }
        
        public void Dispose() {
            for (int i = 0; i < typedFields.Length; i++)
                typedFields[i].Dispose();
            removedKey.Dispose(Untracked.Bytes);
        }
    }
}
