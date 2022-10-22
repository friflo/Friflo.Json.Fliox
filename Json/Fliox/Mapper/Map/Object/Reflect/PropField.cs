// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Mapper.Map.Object.Reflect
{
    public sealed class PropField<T> : PropField
    {
        public PropField(string name, string jsonName, TypeMapper fieldType, FieldInfo field, PropertyInfo property,
            int primIndex, int objIndex, bool required, string docs)
            : base(name, jsonName, fieldType, field, property, CreateMember(fieldType, field, property), primIndex, objIndex, required, docs)
        {
        }
        
        private static Var.Member CreateMember (TypeMapper fieldType, FieldInfo field, PropertyInfo property) {
            if (field != null) {
                return new MemberField(fieldType.varType, field);
            }
            var member = fieldType.varType.CreateMember<T>(property);
            if (member != null)
                return member;
            // object (string, structs, classes) are using a generic MemberProperty  
            return new MemberProperty(fieldType.varType, property);
        }
    }

    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class PropField : IDisposable
    {
        public   readonly   string          name;
        public   readonly   JsonKey         key;
        public   readonly   string          jsonName;

        // ReSharper disable once UnassignedReadonlyField
        // field ist set via reflection to enable using a readonly field
        public   readonly   TypeMapper      fieldType;          // never null
        public   readonly   VarType         varType;            // never null
        public   readonly   int             primIndex;
        public   readonly   int             objIndex;
        public   readonly   bool            required;
        public   readonly   string          docs;
        public   readonly   string          relation;
        internal            Bytes           nameBytes;          // don't mutate
        public              Bytes           firstMember;        // don't mutate
        public              Bytes           subSeqMember;       // don't mutate
        //
        internal readonly   FieldInfo                           field;
        internal readonly   PropertyInfo                        property;
        internal readonly   IEnumerable<CustomAttributeData>    customAttributes;
    //  private  readonly   MethodInfo                          getMethod;
    //  private  readonly   Func<object, object>                getLambda;
    //  private  readonly   Delegate                            getDelegate;
    //  private  readonly   MethodInfo                          setMethod;
    //  private  readonly   Action<object, object>              setLambda;
        internal readonly   Var.Member                          member;


        internal PropField (string name, string jsonName, TypeMapper fieldType, FieldInfo field, PropertyInfo property, Var.Member member,
            int primIndex, int objIndex, bool required, string docs)
        {
            this.name       = name;
            this.key        = new JsonKey(name);
            this.jsonName   = jsonName;
            this.fieldType  = fieldType;
            this.varType    = VarType.FromType(fieldType.type);
            this.nameBytes  = new Bytes(jsonName,                   Untracked.Bytes);
            firstMember     = new Bytes($"{'{'}\"{jsonName}\":",    Untracked.Bytes);
            subSeqMember    = new Bytes($",\"{jsonName}\":",        Untracked.Bytes);
            //
            this.field      = field;
            this.property   = property;
            customAttributes= field != null ? field.CustomAttributes : property.CustomAttributes;
            // this.getMethod  = property != null ? property.GetGetMethod(true) : null;
            // this.setMethod  = property != null ? property.GetSetMethod(true) : null;
            /* if (property != null) {
                // var typeArray    = new [] {  property.DeclaringType, property.PropertyType  };
                // var delegateType = Expression.GetDelegateType(typeArray);
                // getDelegate      =  Delegate.CreateDelegate(delegateType, getMethod);
                var getLambdaExp    = DelegateUtils.CreateGetLambda<object,object>(property);
                var setLambdaExp    = DelegateUtils.CreateSetLambda<object,object>(property);
                getLambda           = getLambdaExp.Compile();
                setLambda           = setLambdaExp.Compile();
            } */
            this.member     = member;
            this.primIndex  = primIndex;
            this.objIndex   = objIndex;
            this.required   = required;
            this.docs       = docs;
            this.relation   = GetRelationAttributeType();
        }
        
        public MemberInfo   Member { get {
            if (field != null)
                return field;
            return property;
        } }

        public void Dispose() {
            subSeqMember.Dispose(Untracked.Bytes);
            firstMember.Dispose(Untracked.Bytes);
            nameBytes.Dispose(Untracked.Bytes);
        }
        
        
        public override string ToString() {
            return name;
        }
        
        private string GetRelationAttributeType() {
            foreach (var attr in customAttributes) {
                if (attr.AttributeType == typeof(RelationAttribute))
                    return (string)attr.ConstructorArguments[0].Value;
            }
            return null;
        }
    }
}
