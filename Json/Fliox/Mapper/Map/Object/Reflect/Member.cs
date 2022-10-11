// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Object.Reflect
{
    // --- fields
    internal class MemberField : Var.Member {
        private  readonly   VarType     varType;
        private  readonly   FieldInfo   field;
        
        internal MemberField(VarType varType, FieldInfo field) {
            this.varType    = varType;
            this.field      = field;
        }
        
        internal    override    Var     GetVar (object obj) {
            // if (useDirect) return field.GetValueDirect(__makeref(obj));
            var value = field.GetValue(obj);
            return varType.FromObject(value);
        }
        
        internal    override    void    SetVar (object obj, in Var value) {
            var valueObject = varType.ToObject(value);
            // if (useDirect) { field.SetValueDirect(__makeref(obj), value); return; }
            field.SetValue(obj, valueObject); // todo use Expression - but not for Unity
        }
    }
    
    // --- properties
    internal class MemberProperty : Var.Member {
        private  readonly   VarType                 varType;
        private  readonly   Func<object, object>    getLambda;
        private  readonly   Action<object, object>  setLambda;
        
        internal MemberProperty(VarType varType, PropertyInfo property) {
            this.varType        = varType;
            var getLambdaExp    = DelegateUtils.CreateGetLambda<object,object>(property);
            var setLambdaExp    = DelegateUtils.CreateSetLambda<object,object>(property);
            getLambda           = getLambdaExp.Compile();
            setLambda           = setLambdaExp.Compile();
        }
        
        internal    override    Var     GetVar (object obj) {
            var value = getLambda(obj); // return new Var(getMethod.Invoke(obj, null));
            return varType.FromObject(value);
        }
        
        internal    override    void    SetVar (object obj, in Var value) {
            var valueObject = varType.ToObject(value);
            setLambda(obj, valueObject);
        }
    }
}