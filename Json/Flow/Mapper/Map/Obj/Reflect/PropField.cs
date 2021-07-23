// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Mapper.Map.Obj.Reflect
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PropField : IDisposable
    {
        public   readonly   string          name;
        public   readonly   string          jsonName;

        // ReSharper disable once UnassignedReadonlyField
        // field ist set via reflection to enable using a readonly field
        public   readonly   TypeMapper      fieldType;          // never null
        public   readonly   int             primIndex;
        public   readonly   int             objIndex;
        public   readonly   bool            required;
        internal            Bytes           nameBytes;          // dont mutate
        public              Bytes           firstMember;        // dont mutate
        public              Bytes           subSeqMember;       // dont mutate
        //
        internal readonly   FieldInfo       field;
        internal readonly   PropertyInfo    property;      
        private  readonly   MethodInfo      getMethod;
        private  readonly   MethodInfo      setMethod;

        internal PropField (string name, string jsonName, TypeMapper fieldType, FieldInfo field, PropertyInfo property,
            int primIndex, int objIndex, bool required)
        {
            this.name       = name;
            this.jsonName   = jsonName;
            this.fieldType  = fieldType;
            this.nameBytes  = new Bytes(jsonName);
            firstMember     = new Bytes($"{'{'}\"{jsonName}\":");
            subSeqMember    = new Bytes($",\"{jsonName}\":");
            //
            this.field      = field;
            this.property   = property;
            this.getMethod  = property != null ? property.GetGetMethod(true) : null;
            this.setMethod  = property != null ? property.GetSetMethod(true) : null;
            this.primIndex  = primIndex;
            this.objIndex   = objIndex;
            this.required   = required;
        }

        public void Dispose() {
            subSeqMember.Dispose();
            firstMember.Dispose();
            nameBytes.Dispose();
        }
        
        private static readonly bool useDirect = false; // Unity: System.NotImplementedException : GetValueDirect
        
        /// <see cref="setMethodParams"/> need to be of Length 1
        public void SetField (object obj, object value, object[] setMethodParams)
        {
            if (field != null) {
                if (useDirect)
                    field.SetValueDirect(__makeref(obj), value);
                else
                    field.SetValue(obj, value);
            } else {
                setMethodParams[0] = value;
                setMethod.Invoke(obj, setMethodParams);
            }
        }
        
        // ReSharper disable PossibleNullReferenceException
        public object GetField (object obj)
        {
            if (field != null) {
                if (useDirect)
                    return field.GetValueDirect(__makeref(obj));
                return field.GetValue (obj);
            }
            return getMethod.Invoke(obj, null);
        }

        public override string ToString() {
            return name;
        }
    }
}
